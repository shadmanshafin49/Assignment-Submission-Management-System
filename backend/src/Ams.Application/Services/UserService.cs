using Ams.Application.Abstractions;
using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ams.Application.Services;

public class UserService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<UserService> logger) : IUserService
{
    public async Task<PagedResult<UserDto>> ListAsync(
        UserListQuery query, CancellationToken ct = default)
    {
        EnsureAdmin();

        var q = db.Users.AsQueryable();

        if (query.Role is { } role)
            q = q.Where(u => u.Role == role);

        if (query.IsActive is { } isActive)
            q = q.Where(u => u.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(u => u.FullName.ToLower().Contains(term)
                             || u.FullNameEn.ToLower().Contains(term)
                             || u.Email.Contains(term));
        }

        var paged = await q
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .ToPagedResultAsync(query.Page, query.PageSize, ct);

        var items = paged.Items.Select(u => u.ToDto()).ToList();
        return PagedResult<UserDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        EnsureAdmin();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException($"ব্যবহারকারী {id} পাওয়া যায়নি।");

        return user.ToDto();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        // RULE: only an admin may create accounts or hand out roles. There is deliberately
        // no public self-registration endpoint — that would let anyone claim a Teacher role.
        EnsureAdmin();

        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new BusinessRuleException($"'{email}' ইমেইলে একটি অ্যাকাউন্ট ইতিমধ্যে রয়েছে।");

        ValidateRoleFields(request.Role, request.Faith);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            FullNameEn = request.FullNameEn.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = request.Role,
            Designation = Normalise(request.Designation),
            Faith = request.Faith,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Admin {AdminId} created {Role} account {UserId}",
            currentUser.UserId, user.Role, user.Id);

        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(
        Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException($"ব্যবহারকারী {id} পাওয়া যায়নি।");

        // RULE: an admin cannot lock themselves out or demote their own account, which would
        // leave the system with no way back in.
        if (user.Id == currentUser.UserId)
        {
            if (!request.IsActive)
                throw new BusinessRuleException("নিজের অ্যাকাউন্ট নিষ্ক্রিয় করা যাবে না।");

            if (request.Role != user.Role)
                throw new BusinessRuleException("নিজের ভূমিকা পরিবর্তন করা যাবে না।");
        }

        // RULE: changing a user's role would orphan the rows tied to their old one.
        if (request.Role != user.Role)
            await EnsureRoleChangeIsSafeAsync(user, ct);

        ValidateRoleFields(request.Role, request.Faith);

        // RULE: a student on a roster cannot lose their faith group — the religion course they
        // take is derived from it, and clearing it would silently drop that course from their feed.
        if (user.Role == UserRole.Student
            && user.Faith is not null
            && request.Faith != user.Faith
            && await db.Enrollments.AnyAsync(e => e.StudentId == user.Id, ct))
        {
            throw new BusinessRuleException(
                "শ্রেণিতে ভর্তি থাকা শিক্ষার্থীর ধর্ম পরিবর্তন করা যাবে না।");
        }

        user.FullName = request.FullName.Trim();
        user.FullNameEn = request.FullNameEn.Trim();
        user.Role = request.Role;
        user.Designation = Normalise(request.Designation);
        user.Faith = request.Faith;
        user.IsActive = request.IsActive;
        user.UpdatedAt = timeProvider.GetUtcNow();

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin {AdminId} updated user {UserId}", currentUser.UserId, id);

        return user.ToDto();
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        EnsureAdmin();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException($"ব্যবহারকারী {id} পাওয়া যায়নি।");

        if (user.Id == currentUser.UserId)
            throw new BusinessRuleException("নিজের অ্যাকাউন্ট নিষ্ক্রিয় করা যাবে না।");

        // RULE: a teacher still holding courses cannot be deactivated — their courses would be
        // left with a login that no longer works and nobody able to set or mark work.
        if (user.Role == UserRole.Teacher
            && await db.Courses.AnyAsync(c => c.TeacherId == id && c.IsActive, ct))
        {
            throw new BusinessRuleException(
                "এই শিক্ষক এখনও কোর্সে নিয়োজিত। আগে কোর্সগুলোতে অন্য শিক্ষক নিয়োগ দিন।");
        }

        // Accounts are deactivated rather than deleted so assignments and submissions keep
        // their authorship. Inactive users are refused at login.
        user.IsActive = false;
        user.UpdatedAt = timeProvider.GetUtcNow();

        // Revoke outstanding sessions so deactivation takes effect immediately.
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in tokens)
            t.RevokedAt = timeProvider.GetUtcNow();

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin {AdminId} deactivated user {UserId}", currentUser.UserId, id);
    }

    private async Task EnsureRoleChangeIsSafeAsync(User user, CancellationToken ct)
    {
        if (user.Role == UserRole.Teacher
            && await db.Assignments.AnyAsync(a => a.CreatedByTeacherId == user.Id, ct))
        {
            throw new BusinessRuleException(
                "এই শিক্ষকের অ্যাসাইনমেন্ট রয়েছে, তাই ভূমিকা পরিবর্তন করা যাবে না। "
                + "পরিবর্তে অ্যাকাউন্টটি নিষ্ক্রিয় করুন।");
        }

        if (user.Role == UserRole.Student
            && await db.Submissions.AnyAsync(s => s.StudentId == user.Id, ct))
        {
            throw new BusinessRuleException(
                "এই শিক্ষার্থীর জমা রয়েছে, তাই ভূমিকা পরিবর্তন করা যাবে না। "
                + "পরিবর্তে অ্যাকাউন্টটি নিষ্ক্রিয় করুন।");
        }
    }

    /// <summary>
    /// Designation belongs to teachers and faith group to students. Enforcing that here keeps
    /// rows meaningful — a student with a designation, or an admin with a religion stream,
    /// would be data nothing in the app knows how to interpret.
    /// </summary>
    private static void ValidateRoleFields(UserRole role, FaithGroup? faith)
    {
        if (role != UserRole.Student && faith is not null)
            throw new ValidationFailedException("কেবল শিক্ষার্থীর জন্য ধর্ম নির্ধারণ করা যায়।");
    }

    private static string? Normalise(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void EnsureAdmin()
    {
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("কেবল প্রশাসক ব্যবহারকারী পরিচালনা করতে পারেন।");
    }
}
