using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Porla
{
    public class PorlaSettingsValidator : AbstractValidator<PorlaSettings>
    {
        public PorlaSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);
            RuleFor(c => c.UrlBase).ValidUrlBase().When(c => c.UrlBase.IsNotNullOrWhiteSpace());

            RuleFor(c => c.TvCategory).Matches(@"^([^\\\/](\/?[^\\\/])*)?$").WithMessage(@"Can not contain '\', '//', or start/end with '/'");
            RuleFor(c => c.TvImportedCategory).Matches(@"^([^\\\/](\/?[^\\\/])*)?$").WithMessage(@"Can not contain '\', '//', or start/end with '/'");
        }
    }

    public class PorlaSettings : IProviderConfig
    {
        private static readonly PorlaSettingsValidator Validator = new PorlaSettingsValidator();

        public IDictionary<string, PorlaPreset> PresetsList;

        public PorlaSettings()
        {
            Host = "localhost";
            SavePath = "/data/downloads";
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Use SSL", Type = FieldType.Checkbox, HelpText = "Use a secure connection.")]
        public bool UseSsl { get; set; }

        [FieldDefinition(3, Label = "Url Base", Type = FieldType.Textbox, Advanced = true, HelpText = "Adds a prefix to the Porla url, e.g. http://[host]:[port]/[urlBase]/api.")]
        public string UrlBase { get; set; }

        [FieldDefinition(4, Label = "JWT Token", Type = FieldType.Textbox, HelpText = "Your generated authorization token")]
        public string Token { get; set; }

        [FieldDefinition(5, Label = "Preset", Type = FieldType.Select, SelectOptionsProviderAction = "getPresets", HelpText = "Porla preset to use for downloads")]
        public IEnumerable<string> Presets { get; set; }

        [FieldDefinition(6, Label = "Save Path", Type = FieldType.Path, HelpText = "The path Porla will download files to")]
        public string SavePath { get; set; }

        [FieldDefinition(7, Label = "Category", Type = FieldType.Textbox, HelpText = "Adding a category specific to Sonarr avoids conflicts with unrelated non-Sonarr downloads. Using a category is optional, but strongly recommended.")]
        public string TvCategory { get; set; }

        [FieldDefinition(8, Label = "Post-Import Category", Type = FieldType.Textbox, Advanced = true, HelpText = "Category for Sonarr to set after it has imported the download. Sonarr will not remove the torrent if seeding has finished. Leave blank to keep same category.")]
        public string TvImportedCategory { get; set; }
        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
