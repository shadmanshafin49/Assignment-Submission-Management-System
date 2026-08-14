using Ams.Application.Dtos;
using FluentValidation;

namespace Ams.Application.Validators;

public class CreateSubmissionRequestValidator : AbstractValidator<CreateSubmissionRequest>
{
    public CreateSubmissionRequestValidator()
    {
        // Deliberately not NotEmpty: a student may answer entirely with attached files — a
        // photographed page of maths working is the normal case. "Text or at least one file"
        // is enforced in SubmissionService, where the files are visible.
        RuleFor(x => x.AnswerText).MaximumLength(20000);
    }
}

public class UpdateSubmissionRequestValidator : AbstractValidator<UpdateSubmissionRequest>
{
    public UpdateSubmissionRequestValidator()
    {
        RuleFor(x => x.AnswerText).MaximumLength(20000);
    }
}

public class GradeSubmissionRequestValidator : AbstractValidator<GradeSubmissionRequest>
{
    public GradeSubmissionRequestValidator()
    {
        // The upper bound depends on the assignment's MaxMarks, so it is checked in
        // SubmissionService.GradeAsync where that value is available.
        RuleFor(x => x.Marks).GreaterThanOrEqualTo(0).WithMessage("নম্বর ঋণাত্মক হতে পারে না।");
        RuleFor(x => x.Feedback).MaximumLength(5000);
    }
}

public class UpdateSubmissionStatusRequestValidator : AbstractValidator<UpdateSubmissionStatusRequest>
{
    public UpdateSubmissionStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
