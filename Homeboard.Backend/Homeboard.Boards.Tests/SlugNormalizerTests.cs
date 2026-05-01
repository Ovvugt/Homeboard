using Homeboard.Boards.Services;
using NUnit.Framework;

namespace Homeboard.Boards.Tests;

[TestFixture]
public sealed class SlugNormalizerTests
{
    [TestCase("Home", "home")]
    [TestCase("My Home Board", "my-home-board")]
    [TestCase("  Trimmed  ", "trimmed")]
    [TestCase("Café au lait", "cafe-au-lait")]
    [TestCase("Hi!@#$ there", "hi-there")]
    [TestCase("multi---hyphens", "multi-hyphens")]
    public void Normalize_known_inputs(string input, string expected)
    {
        Assert.That(SlugNormalizer.Normalize(input), Is.EqualTo(expected));
    }
}
