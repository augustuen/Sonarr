using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.CustomFormats
{
    public class BitrateSpecificationValidator : AbstractValidator<BitrateSpecification>
    {
        public BitrateSpecificationValidator()
        {
            RuleFor(c => c.Min).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Max).GreaterThan(c => c.Min);
        }
    }

    public class BitrateSpecification : CustomFormatSpecificationBase
    {
        private static readonly BitrateSpecificationValidator Validator = new BitrateSpecificationValidator();
        public override int Order => 8;
        public override string ImplementationName => "Bitrate";

        [FieldDefinition(1, Label = "CustomFormatsSpecificationMinimumBitrate", HelpText = "CustomFormatsSpecificationMinimumBitrateHelpText", Unit = "GB", Type = FieldType.Number)]
        public double Min { get; set; }

        [FieldDefinition(1, Label = "CustomFormatsSpecificationMaxBitrate", HelpText = "CustomFormatsSpecificationMaxBitrateHelpText", Unit = "GiB/hour", Type = FieldType.Number)]
        public double Max { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            var size = input.Size;
            var numberOfEpisodes = input.EpisodeInfo.EpisodeNumbers.Length > 0 ? input.EpisodeInfo.EpisodeNumbers.Length : 1;
            var totalRuntime = input.Series.Runtime * numberOfEpisodes;

            return (size * 60) / totalRuntime > Min.Gigabytes() && (size * 60) / totalRuntime <= Max.Gigabytes();
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
