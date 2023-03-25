using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.StableDiffusion.WebUi.Api.Models
{
    public static class Extensions
    {
        public static Text2Img EnableLoraBlockWeight(this Text2Img arg)
        {
            arg.ExtensionArguments.Add("LoRA Block Weight", new { });
            return arg;
        }

        public static Text2Img EnabledComposableLora(this Text2Img arg)
        {
            arg.ExtensionArguments.Add("Composable Lora", new Text2Img.ScriptArgument() { Args = new() { true, false, false } });
            return arg;
        }

        public class LatentCouple
        {
            public string Divisions { get; set; }
            public string Positions { get; set; }
            public string Weights { get; set; }
            public int EndAtStep { get; set; }

            public static LatentCouple LeftRight(int endStep)
            {
                return new LatentCouple
                {
                    Divisions = "1:1,1:2,1:2",
                    Positions = "0:0,0:0,0:1",
                    Weights = "0.2,0.8,0.8",
                    EndAtStep = endStep
                };
            }

            public static LatentCouple TopDown(int endStep)
            {
                return new LatentCouple
                {
                    Divisions = "1:1,2:1,2:1",
                    Positions = "0:0,0:0,1:0",
                    Weights = "0.2,0.8,0.8",
                    EndAtStep = endStep
                };
            }

            public static LatentCouple Cross(int endStep)
            {
                return new LatentCouple
                {
                    Divisions = "1:1,2:2,2:2,2:2,2:2",
                    Positions = "0:0,0:0,1:1,0:1,1:0",
                    Weights = "0.2,0.8,0.8,0.8,0.8",
                    EndAtStep = endStep
                };
            }
        }

        public static Text2Img EnableLatentCouple(this Text2Img arg, LatentCouple couple)
        {
            arg.ExtensionArguments.Add("Latent Couple extension", new Text2Img.ScriptArgument() { Args = new() {
                true, couple.Divisions, couple.Positions, couple.Weights, couple.EndAtStep }
            });

            return arg;
        }

        public static Text2Img EnableHiresScale(this Text2Img arg, float factor)
        {
            arg.EnableHiresFix = true;
            arg.HiResFixScale = factor;
            arg.HiResFixUpScalar = "Latent";
            arg.HiResSecondPassSteps = 30;
            arg.HiResFixDenoisingStrength = 0.6f;
            return arg;
        }

        public static Text2Img Size(this Text2Img arg, int width, int height)
        {
            arg.Width = width;
            arg.Height = height;
            return arg;
        }
    }
}
