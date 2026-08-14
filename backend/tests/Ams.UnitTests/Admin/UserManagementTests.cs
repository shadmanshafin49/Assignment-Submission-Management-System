using Ams.Application.Dtos;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Ams.UnitTests.Infrastructure;
using Shouldly;

namespace Ams.UnitTests.Admin;

/// <summary>Account management is admin-only, and an admin cannot lock themselves out.</summary>
public class UserManagementTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly TestWorld _world;

    public UserManagementTests() => _world = new TestWorld(_ctx);

    public void Dispose() => _ctx.Dispose();

    private static CreateUserRequest NewTeacher(string email = "notun.shikkhok@gcbhs.edu.bd") =>
        new("মোঃ নতুন শিক্ষক", "Md Notun Shikkhok", email, "Password@123",
            UserRole.Teacher, "সহকারী শিক্ষক", null);

    [Fact]
    public async Task An_admin_can_create_a_user()
    {
        var result = await _world.UsersAs(_world.Admin).CreateAsync(NewTeacher());

        result.Email.ShouldBe("notun.shikkhok@gcbhs.edu.bd");
        result.Role.ShouldBe(UserRole.Teacher);
        result.Designation.ShouldBe("সহকারী শিক্ষক");
        result.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Created_users_have_their_password_hashed_not_stored_in_plain_text()
    {
        await _world.UsersAs(_world.Admin).CreateAsync(NewTeacher());

        _ctx.ClearTracking();
        var stored = _ctx.Db.Users.Single(u => u.Email == "notun.shikkhok@gcbhs.edu.bd");

        stored.PasswordHash.ShouldNotBe("Password@123");
        stored.PasswordHash.ShouldNotContain("Password");
        // PBKDF2 format is iterations.salt.subkey
        stored.PasswordHash.Split('.').Length.ShouldBe(3);
    }

    [Fact]
    public async Task Email_addresses_are_normalised_to_lower_case()
    {
        await _world.UsersAs(_world.Admin).CreateAsync(new CreateUserRequest(
            "মিশ্র", "Mixed Case", "MiXeD@GCBHS.edu.BD", "Password@123",
            UserRole.Student, null, FaithGroup.Islam));

        _ctx.ClearTracking();
        _ctx.Db.Users.Any(u => u.Email == "mixed@gcbhs.edu.bd").ShouldBeTrue();
    }

    [Fact]
    public async Task Duplicate_email_addresses_are_refused()
    {
        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.UsersAs(_world.Admin).CreateAsync(NewTeacher("rejaul@gcbhs.edu.bd")));

        ex.Message.ShouldContain("ইতিমধ্যে");
    }

    [Fact]
    public async Task A_faith_group_belongs_to_students_only()
    {
        // A teacher with a religion stream would be data nothing in the app knows how to read —
        // faith drives which religion course a *student* takes, and nothing else.
        var request = NewTeacher() with { Faith = FaithGroup.Islam };

        await Should.ThrowAsync<ValidationFailedException>(
            () => _world.UsersAs(_world.Admin).CreateAsync(request));
    }

    [Theory]
    [InlineData(UserRole.Teacher)]
    [InlineData(UserRole.Student)]
    public async Task Non_admins_cannot_create_users(UserRole role)
    {
        // Without this, anyone could mint themselves an Admin account.
        var actor = role == UserRole.Teacher ? _world.Rejaul : _world.Sadman;

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.UsersAs(actor).CreateAsync(NewTeacher()));
    }

    [Theory]
    [InlineData(UserRole.Teacher)]
    [InlineData(UserRole.Student)]
    public async Task Non_admins_cannot_list_users(UserRole role)
    {
        var actor = role == UserRole.Teacher ? _world.Rejaul : _world.Sadman;

        await Should.ThrowAsync<ForbiddenException>(
            () => _world.UsersAs(actor).ListAsync(new UserListQuery()));
    }

    [Fact]
    public async Task An_admin_cannot_deactivate_their_own_account()
    {
        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.UsersAs(_world.Admin).DeactivateAsync(_world.Admin.Id));

        ex.Message.ShouldContain("নিজের অ্যাকাউন্ট");
    }

    [Fact]
    public async Task An_admin_cannot_change_their_own_role()
    {
        // Demoting the last admin would leave nobody able to administer the system.
        var request = new UpdateUserRequest(
            "প্রধান শিক্ষক", "Head Teacher", UserRole.Student, null, null, true);

        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.UsersAs(_world.Admin).UpdateAsync(_world.Admin.Id, request));
    }

    [Fact]
    public async Task A_teacher_still_holding_courses_cannot_be_deactivated()
    {
        // Their courses would be left with a login that no longer works and nobody able to set
        // or mark work.
        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.UsersAs(_world.Admin).DeactivateAsync(_world.Rejaul.Id));

        ex.Message.ShouldContain("কোর্সে নিয়োজিত");
    }

    [Fact]
    public async Task Deactivating_a_user_keeps_the_record_rather_than_deleting_it()
    {
        await _world.UsersAs(_world.Admin).DeactivateAsync(_world.Tanvir.Id);

        _ctx.ClearTracking();
        var stored = _ctx.Db.Users.Single(u => u.Id == _world.Tanvir.Id);
        stored.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task A_teachers_role_cannot_be_changed_once_they_own_assignments()
    {
        _world.GivenAssignment(teacher: _world.Rejaul);

        var request = new UpdateUserRequest(
            "মোঃ রেজাউল করিম", "Md Rejaul Karim", UserRole.Student, null, FaithGroup.Islam, true);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.UsersAs(_world.Admin).UpdateAsync(_world.Rejaul.Id, request));

        ex.Message.ShouldContain("ভূমিকা পরিবর্তন করা যাবে না");
    }

    [Fact]
    public async Task A_students_role_cannot_be_changed_once_they_have_submissions()
    {
        var assignment = _world.GivenAssignment();
        _world.GivenSubmission(assignment, _world.Sadman);

        var request = new UpdateUserRequest(
            "মোঃ সাদমান সাকিব", "Md Sadman Sakib", UserRole.Teacher, null, null, true);

        await Should.ThrowAsync<BusinessRuleException>(
            () => _world.UsersAs(_world.Admin).UpdateAsync(_world.Sadman.Id, request));
    }

    [Fact]
    public async Task An_enrolled_students_faith_group_cannot_be_changed()
    {
        // The religion course they take is derived from it; changing it would silently swap
        // their course and orphan the work they have already done in the old one.
        var request = new UpdateUserRequest(
            "মোঃ সাদমান সাকিব", "Md Sadman Sakib", UserRole.Student, null, FaithGroup.Hindu, true);

        var ex = await Should.ThrowAsync<BusinessRuleException>(
            () => _world.UsersAs(_world.Admin).UpdateAsync(_world.Sadman.Id, request));

        ex.Message.ShouldContain("ধর্ম পরিবর্তন করা যাবে না");
    }
}
