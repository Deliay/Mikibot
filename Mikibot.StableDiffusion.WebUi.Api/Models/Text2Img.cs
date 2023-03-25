using System.Text.Json.Serialization;

namespace Mikibot.StableDiffusion.WebUi.Api.Models
{
    public class Text2Img
    {

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("negative_prompt")]
        public string NegativePrompt { get; set; }

        [JsonPropertyName("cfg_scale")]
        public double CfgScale { get; set; }

        [JsonPropertyName("steps")]
        public int Steps { get; set; }

        [JsonPropertyName("enable_hr")]
        public bool EnableHiresFix { get; set; }

        [JsonPropertyName("sampler_index")]
        public string Sampler { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("denoising_strength")]
        public float HiResFixDenoisingStrength { get; set; }

        [JsonPropertyName("hr_scale")]
        public float HiResFixScale { get; set; }

        [JsonPropertyName("hr_upscaler")]
        public string HiResFixUpScalar { get; set; }

        [JsonPropertyName("hr_second_pass_steps")]
        public int HiResSecondPassSteps { get; set; }

        public class ScriptArgument
        {
            [JsonPropertyName("args")]
            public List<object>? Args { get; set; }
        }

        [JsonPropertyName("alwayson_scripts")]
        public Dictionary<string, object> ExtensionArguments { get; set; } = new();
    }
}