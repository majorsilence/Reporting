// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using Majorsilence.Forms;
using Majorsilence.WinformUtils;
using NUnit.Framework;

namespace Majorsilence.WinformUtils.Forms.Tests;

// These tests exercise Application.OpenForms (global process state) and must run serially --
// NUnit runs tests within a fixture sequentially by default, which is what we want here.
[TestFixture]
public class WaitFormTests
{
    [Test]
    public void ShowWaiter_OnForm_OpensAnOwnedWaitFormWithoutThrowing()
    {
        using var owner = new Form { Size = new System.Drawing.Size(400, 300) };
        owner.Show();

        int countBefore = Application.OpenForms.Count;
        Assert.DoesNotThrow(() => owner.ShowWaiter());

        // WaitForm is internal to Majorsilence.WinformUtils, so this test (a separate assembly)
        // can only observe it by name via Application.OpenForms, not by type.
        bool hasWaitForm = Application.OpenForms.Cast<Form>().Any(f => f.GetType().Name == "WaitForm");
        Assert.That(hasWaitForm, Is.True, "ShowWaiter() should have opened an internal WaitForm");
        Assert.That(Application.OpenForms.Count, Is.EqualTo(countBefore + 1));

        owner.HideWaiter();
    }

    [Test]
    public void ShowWaiter_TwiceInARow_IsIdempotent()
    {
        using var owner = new Form { Size = new System.Drawing.Size(400, 300) };
        owner.Show();

        owner.ShowWaiter();
        int countAfterFirst = Application.OpenForms.Count;

        Assert.DoesNotThrow(() => owner.ShowWaiter());
        Assert.That(Application.OpenForms.Count, Is.EqualTo(countAfterFirst),
            "a second ShowWaiter() call while one is already open should be a no-op, not open a second overlay");

        owner.HideWaiter();
    }

    [Test]
    public void HideWaiter_WithoutAPriorShowWaiter_DoesNotThrow()
    {
        using var owner = new Form { Size = new System.Drawing.Size(400, 300) };
        owner.Show();

        Assert.DoesNotThrow(() => owner.HideWaiter());
    }

    [Test]
    public void ShowWaiter_ThenHideWaiter_ClosesTheOverlay()
    {
        using var owner = new Form { Size = new System.Drawing.Size(400, 300) };
        owner.Show();

        owner.ShowWaiter();
        int countWhileOpen = Application.OpenForms.Count;

        owner.HideWaiter();

        Assert.That(Application.OpenForms.Count, Is.LessThan(countWhileOpen));
    }

    [Test]
    public void ShowWaiter_OnUserControlsOwnerForm_DelegatesToTheForm()
    {
        using var owner = new Form { Size = new System.Drawing.Size(400, 300) };
        var child = new UserControl { Size = new System.Drawing.Size(200, 150) };
        owner.Controls.Add(child);
        owner.Show();

        Assert.DoesNotThrow(() => child.ShowWaiter());
        owner.HideWaiter();
    }
}
