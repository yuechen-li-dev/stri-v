using System;
using System.Collections.Generic;
using Xunit;

namespace Stride.Input.Tests;

public class GestureEventPipelineTests
{
    [Fact]
    public void InputEventPool_GetOrCreate_ThenEnqueue_ClearsDeviceAndReusesInstance()
    {
        var first = InputEventPool<KeyEvent>.GetOrCreate(null!);
        InputEventPool<KeyEvent>.Enqueue(first);

        var second = InputEventPool<KeyEvent>.GetOrCreate(null!);
        Assert.Same(first, second);

        InputEventPool<KeyEvent>.Enqueue(second);
    }

    [Fact]
    public void GestureRecognizerTap_TwoTapFrames_DoNotEmitWithoutTimeoutFlush()
    {
        var recognizer = new GestureRecognizerTap(new GestureConfigTap(2, 1), screenRatio: 1f);
        var output = new List<GestureEvent>();

        recognizer.ProcessPointerEvents(TimeSpan.FromMilliseconds(16), new List<PointerEvent>
        {
            CreatePointer(1, PointerEventType.Pressed, 0.5f, 0.5f),
            CreatePointer(1, PointerEventType.Released, 0.5f, 0.5f),
        }, output);

        recognizer.ProcessPointerEvents(TimeSpan.FromMilliseconds(16), new List<PointerEvent>
        {
            CreatePointer(2, PointerEventType.Pressed, 0.5f, 0.5f),
            CreatePointer(2, PointerEventType.Released, 0.5f, 0.5f),
        }, output);

        Assert.Empty(output);
    }

    private static PointerEvent CreatePointer(int pointerId, PointerEventType type, float x, float y)
    {
        return new PointerEvent
        {
            PointerId = pointerId,
            Position = new Stride.Core.Mathematics.Vector2(x, y),
            EventType = type,
        };
    }
}
