using Ams.Application.Dtos;
using FluentValidation;

namespace Ams.Application.Validators;

public class CreateAssignmentRequestValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("শিরোনাম দিন।").MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().WithMessage("নির্দেশনা দিন।").MaximumLength(5000);
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ChapterOrLesson).MaximumLength(200);
        RuleFor(x => x.MaxMarks)
            .GreaterThan(0).WithMessage("পূর্ণ নম্বর শূন্যের বেশি হতে হবে।")
            .LessThanOrEqualTo(1000);

        // "The deadline must be in the future" and "the subject must permit this type" are
        // enforced in AssignmentService, which holds the injected clock and the course. Keeping
        // them there means they are covered by unit tests rather than depending on wall-clock
        // time or a second database round-trip here.
    }
}

public class UpdateAssignmentRequestValidator : AbstractValidator<UpdateAssignmentRequest>
{
    public UpdateAssignmentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ChapterOrLesson).MaximumLength(200);
        RuleFor(x => x.MaxMarks).GreaterThan(0).LessThanOrEqualTo(1000);
    }
}

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("মন্তব্য লিখুন।")
            .MaximumLength(2000);
    }
}
