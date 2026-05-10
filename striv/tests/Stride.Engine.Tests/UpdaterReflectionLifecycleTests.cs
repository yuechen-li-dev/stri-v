using System;
using System.Collections.Generic;
using Stride.Updater;
using Xunit;

namespace Stride.Engine.Tests;

public class UpdaterReflectionLifecycleTests
{
    [Fact]
    public void UpdateEngineCompile_MissingMember_ThrowsDeterministicInvalidOperation()
    {
        var updateMembers = new List<UpdateMemberInfo>
        {
            new("MissingMember", 0),
        };

        var exception = Assert.Throws<InvalidOperationException>(() => UpdateEngine.Compile(typeof(UpdateTarget), updateMembers));
        Assert.Contains("could not find binding info for member MissingMember", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateEngineCompile_RegisteredMember_CompilesWithoutNullReferenceFailures()
    {
        UpdateEngine.RegisterMember(typeof(UpdateTarget), nameof(UpdateTarget.Value), new UpdatableTestMember());

        var updateMembers = new List<UpdateMemberInfo>
        {
            new(nameof(UpdateTarget.Value), 4),
        };

        var compiled = UpdateEngine.Compile(typeof(UpdateTarget), updateMembers);

        Assert.Empty(compiled.UpdateOperations);
    }

    private sealed class UpdateTarget
    {
        public int Value = 42;
    }

    private sealed class UpdatableTestMember : UpdatableMember
    {
        public override Type MemberType => typeof(int);
    }
}
