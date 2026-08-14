using Ams.Application.Dtos;
using Ams.Domain.Entities;
using FluentValidation;

namespace Ams.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("নাম দিন।").MaximumLength(150);
        RuleFor(x => x.FullNameEn).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Designation).MaximumLength(120);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("পাসওয়ার্ড অন্তত ৮ অক্ষরের হতে হবে।")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("পাসওয়ার্ডে একটি বড় হাতের অক্ষর থাকতে হবে।")
            .Matches("[a-z]").WithMessage("পাসওয়ার্ডে একটি ছোট হাতের অক্ষর থাকতে হবে।")
            .Matches("[0-9]").WithMessage("পাসওয়ার্ডে একটি সংখ্যা থাকতে হবে।");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.FullNameEn).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Designation).MaximumLength(120);
    }
}

public class CreateClassRoomRequestValidator : AbstractValidator<CreateClassRoomRequest>
{
    public CreateClassRoomRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Level)
            .InclusiveBetween(1, 12).WithMessage("শ্রেণি ১ থেকে ১২ এর মধ্যে হতে হবে।");
        RuleFor(x => x.Section).MaximumLength(20);
        RuleFor(x => x.AcademicYear).NotEmpty().MaximumLength(20);
    }
}

public class UpdateClassRoomRequestValidator : AbstractValidator<UpdateClassRoomRequest>
{
    public UpdateClassRoomRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Level).InclusiveBetween(1, 12);
        RuleFor(x => x.Section).MaximumLength(20);
        RuleFor(x => x.AcademicYear).NotEmpty().MaximumLength(20);
    }
}

public class CreateSubjectRequestValidator : AbstractValidator<CreateSubjectRequest>
{
    public CreateSubjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(120);

        // Board subject codes are three digits — 101, 109, 154 — so anything else is a typo.
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^\d{3}$").WithMessage("বিষয় কোড তিন অঙ্কের হতে হবে (যেমন ১০১ → 101)।");

        RuleFor(x => x.TextbookName).MaximumLength(120);
        RuleFor(x => x.FullMarks).InclusiveBetween(1, 200);
        RuleFor(x => x.WeeklyPeriods).InclusiveBetween(0, PeriodSchedule.PeriodsPerWeek);
        RuleFor(x => x.AllowedAssignmentTypes)
            .NotEmpty().WithMessage("অন্তত একটি অ্যাসাইনমেন্টের ধরন নির্বাচন করুন।");
        RuleForEach(x => x.AllowedAssignmentTypes).IsInEnum();
    }
}

public class UpdateSubjectRequestValidator : AbstractValidator<UpdateSubjectRequest>
{
    public UpdateSubjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().Matches(@"^\d{3}$");
        RuleFor(x => x.TextbookName).MaximumLength(120);
        RuleFor(x => x.FullMarks).InclusiveBetween(1, 200);
        RuleFor(x => x.WeeklyPeriods).InclusiveBetween(0, PeriodSchedule.PeriodsPerWeek);
        RuleFor(x => x.AllowedAssignmentTypes).NotEmpty();
        RuleForEach(x => x.AllowedAssignmentTypes).IsInEnum();
    }
}

public class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.ClassRoomId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
    }
}

public class CreateEnrollmentRequestValidator : AbstractValidator<CreateEnrollmentRequest>
{
    public CreateEnrollmentRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.ClassRoomId).NotEmpty();
        RuleFor(x => x.RollNumber)
            .GreaterThan(0).WithMessage("রোল নম্বর অবশ্যই ধনাত্মক হতে হবে।")
            .LessThanOrEqualTo(500);
    }
}

public class SetRoutinePeriodRequestValidator : AbstractValidator<SetRoutinePeriodRequest>
{
    public SetRoutinePeriodRequestValidator()
    {
        RuleFor(x => x.ClassRoomId).NotEmpty();
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Day).IsInEnum();
        RuleFor(x => x.PeriodIndex)
            .InclusiveBetween(1, PeriodSchedule.PeriodsPerDay)
            .WithMessage($"পিরিয়ড ১ থেকে {PeriodSchedule.PeriodsPerDay} এর মধ্যে হতে হবে।");
    }
}

public class UpdateAppSettingsRequestValidator : AbstractValidator<UpdateAppSettingsRequest>
{
    public UpdateAppSettingsRequestValidator()
    {
        RuleFor(x => x.Settings).NotEmpty().WithMessage("অন্তত একটি সেটিং দিতে হবে।");
    }
}
