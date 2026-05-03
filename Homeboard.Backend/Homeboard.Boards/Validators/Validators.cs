using FluentValidation;
using Homeboard.Boards.Dtos;

namespace Homeboard.Boards.Validators;

public sealed class CreateBoardDtoValidator : AbstractValidator<CreateBoardDto>
{
    public CreateBoardDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Slug).MaximumLength(80);
        RuleFor(x => x.GridColumns).InclusiveBetween(4, 24).When(x => x.GridColumns.HasValue);
    }
}

public sealed class UpdateBoardDtoValidator : AbstractValidator<UpdateBoardDto>
{
    public UpdateBoardDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(80);
        RuleFor(x => x.GridColumns).InclusiveBetween(4, 24);
    }
}

public sealed class CreateTileDtoValidator : AbstractValidator<CreateTileDto>
{
    public CreateTileDtoValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.GridX).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GridY).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GridW).InclusiveBetween(1, 24);
        RuleFor(x => x.GridH).InclusiveBetween(1, 24);
        RuleFor(x => x.StatusInterval).InclusiveBetween(10, 86400).When(x => x.StatusInterval.HasValue);
        RuleFor(x => x.StatusTimeout).InclusiveBetween(100, 60000).When(x => x.StatusTimeout.HasValue);
    }
}

public sealed class UpdateTileDtoValidator : AbstractValidator<UpdateTileDto>
{
    public UpdateTileDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.StatusInterval).InclusiveBetween(10, 86400);
        RuleFor(x => x.StatusTimeout).InclusiveBetween(100, 60000);
    }
}

public sealed class CreateWidgetDtoValidator : AbstractValidator<CreateWidgetDto>
{
    public CreateWidgetDtoValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.GridW).InclusiveBetween(1, 24);
        RuleFor(x => x.GridH).InclusiveBetween(1, 24);
    }
}

public sealed class CreateSectionDtoValidator : AbstractValidator<CreateSectionDto>
{
    public CreateSectionDtoValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(80);
    }
}

public sealed class UpdateSectionDtoValidator : AbstractValidator<UpdateSectionDto>
{
    public UpdateSectionDtoValidator()
    {
        RuleFor(x => x.Name).MaximumLength(80);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveLayoutDtoValidator : AbstractValidator<SaveLayoutDto>
{
    public SaveLayoutDtoValidator()
    {
        RuleFor(x => x.Items).NotNull();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id).NotEmpty();
            item.RuleFor(i => i.GridX).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.GridY).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.GridW).InclusiveBetween(1, 24);
            item.RuleFor(i => i.GridH).InclusiveBetween(1, 24);
        });
    }
}
