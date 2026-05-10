using Stride.Animations;
using Xunit;

namespace Stride.Engine.Tests;

public class AnimationLifecycleTests
{
    [Fact]
    public void AnimationUpdater_DefaultConstruction_HasValidInertState()
    {
        var updater = new AnimationUpdater();
        var entity = new Stride.Engine.Entity();
        var clipResult = new AnimationClipResult
        {
            Channels = [],
            Data = [],
            Objects = [],
        };

        var exception = Record.Exception(() => updater.Update(entity, clipResult));

        Assert.Null(exception);
    }

    [Fact]
    public void AnimationClip_DefaultConstruction_HasValidEmptyCollections()
    {
        var clip = new AnimationClip();

        Assert.NotNull(clip.Channels);
        Assert.Empty(clip.Channels);
        Assert.NotNull(clip.Curves);
        Assert.Empty(clip.Curves);
        Assert.NotNull(clip.OptimizedAnimationDatas);
        Assert.Empty(clip.OptimizedAnimationDatas);
        Assert.Null(clip.GetCurve("Missing"));
    }

    [Fact]
    public void AnimationProcessor_DefaultConstruction_DoesNotRequireRuntimeServices()
    {
        var processor = new AnimationProcessor();

        Assert.NotNull(processor);
        Assert.Null(processor.GetAnimationClipResult(new AnimationComponent()));
    }

    [Fact]
    public void ComputeBinaryCurve_DefaultConstruction_AllowsMissingChildren()
    {
        var curve = new FloatBinaryCurve();

        Assert.Null(curve.LeftChild);
        Assert.Null(curve.RightChild);
        Assert.Equal(0f, curve.Evaluate(0.5f));
    }

    [Fact]
    public void ComputeCurveSampler_DefaultConstruction_WithoutCurve_EvaluatesDefaultValue()
    {
        var sampler = new FloatCurveSampler();

        Assert.Null(sampler.Curve);
        Assert.Equal(0f, sampler.Evaluate(0.3f));
        Assert.True(sampler.UpdateChanges());
    }

    private sealed class FloatBinaryCurve : ComputeBinaryCurve<float>
    {
        protected override float Add(float a, float b) => a + b;

        protected override float Subtract(float a, float b) => a - b;

        protected override float Multiply(float a, float b) => a * b;
    }

    private sealed class FloatCurveSampler : ComputeCurveSampler<float>
    {
        public override void Linear(ref float value1, ref float value2, float t, out float result)
        {
            result = value1 + ((value2 - value1) * t);
        }
    }
}
