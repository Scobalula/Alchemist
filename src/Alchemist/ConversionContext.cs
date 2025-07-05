using RedFox.Graphics3D;
using RedFox.Graphics3D.Skeletal;
using RedFox.Graphics3D.Translation;
using System.Diagnostics.CodeAnalysis;
using System.IO.Enumeration;

namespace Alchemist
{
    public class ConversionContext
    {
        public required Skeleton Skeleton { get; set; }
        public SkeletonAnimation? LeftHandPose { get; set; }
        public SkeletonAnimation? RightHandPose { get; set; }

        public required string OutputName { get; set; }

        public required string OutputFolder { get; set; }

        public required string OutputFormat { get; set; }

        //public List<Converter> Converts { get; set; } = [];

        public List<SkeletonAnimation> Animations { get; set; } = [];

        public Graphics3DTranslatorFactory TranslatorFactory { get; set; } = new Graphics3DTranslatorFactory().WithDefaultTranslators();

        public bool TryFindAnimation(string pattern, [NotNullWhen(true)] out SkeletonAnimation? animation)
        {
            animation = null;

            foreach (var potential in Animations)
            {
                if(FileSystemName.MatchesSimpleExpression(pattern, potential.Name))
                {
                    animation = potential;
                    return true;
                }
            }

            return false;
        }
    }
}
