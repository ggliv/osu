// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osuTK.Input;

namespace osu.Game.Tests.Visual.UserInterface
{
    [TestFixture]
    public class TestSceneHistoryTextBox : OsuManualInputManagerTestScene
    {
        private const string temp = "Temp message";

        private int messageCounter;

        private HistoryTextBox box = null!;
        private OsuSpriteText text = null!;

        [SetUp]
        public void SetUp()
        {
            Schedule(() =>
            {
                Children = new Drawable[]
                {
                    box = new HistoryTextBox(5)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.X,
                        Width = 0.99f,
                    },
                    text = new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.X,
                        Width = 0.99f,
                        Y = -box.Height,
                        Font = OsuFont.Default.With(size: 20),
                    }
                };

                box.OnCommit += (_, _) =>
                {
                    if (string.IsNullOrEmpty(box.Text))
                        return;

                    text.Text = $"{nameof(box.OnCommit)}: {box.Text}";
                    box.Text = string.Empty;
                    box.TakeFocus();
                    text.FadeOutFromOne(1000, Easing.InQuint);
                };

                messageCounter = 0;

                box.TakeFocus();
            });
        }

        [Test]
        public void TestChangedHistory()
        {
            addMessages(2);
            AddStep("Set text", () => box.Text = temp);
            AddStep("Move up", () => InputManager.Key(Key.Up));

            AddStep("Change text", () => box.Text = "New message");
            AddStep("Move down", () => InputManager.Key(Key.Down));
            AddStep("Move up", () => InputManager.Key(Key.Up));
            AddAssert("Changes lost", () => box.Text == "Message 2");
        }

        [Test]
        public void TestInputOnEdge()
        {
            addMessages(2);
            AddStep("Set text", () => box.Text = temp);

            AddStep("Move down", () => InputManager.Key(Key.Down));
            AddAssert("Text unchanged", () => box.Text == temp);

            AddRepeatStep("Move up", () => InputManager.Key(Key.Up), 2);
            AddAssert("Same as 1st message", () => box.Text == "Message 1");

            AddStep("Move up", () => InputManager.Key(Key.Up));
            AddAssert("Text unchanged", () => box.Text == "Message 1");
        }

        [Test]
        public void TestResetIndex()
        {
            addMessages(2);

            AddRepeatStep("Move Up", () => InputManager.Key(Key.Up), 2);
            AddAssert("Same as 1st message", () => box.Text == "Message 1");

            AddStep("Remove text", () => box.Text = string.Empty);
            AddStep("Move Up", () => InputManager.Key(Key.Up));
            AddAssert("Same as previous message", () => box.Text == "Message 2");

            AddStep("Move Up", () => InputManager.Key(Key.Up));
            AddStep("Select text", () => InputManager.Keys(PlatformAction.SelectAll));
            AddStep("Replace text", () => box.Text = "New text");
            AddStep("Move Up", () => InputManager.Key(Key.Up));
            AddAssert("Same as previous message", () => box.Text == "Message 2");
        }

        private void addMessages(int count)
        {
            AddRepeatStep("Add messages", () =>
            {
                box.Text = $"Message {++messageCounter}";
                InputManager.Key(Key.Enter);
            }, count);
        }
    }
}
