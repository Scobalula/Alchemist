namespace Alchemist.UI
{
    public class GenerateSprintAnimsScript : Script
    {
        public override string Name => "Generate Sprint Anims Script";

        public override void Run(MainViewModel viewModel)
        {
            if (!viewModel.TryFindAnimation("*_idle*", out var idleAnimation))
                return;

            // Sprints
            var sprintIn = idleAnimation.Clone();
            var sprintLoop = idleAnimation.Clone();
            var sprintOut = idleAnimation.Clone();

            sprintIn.OutputName = idleAnimation.OutputName.Replace("_idle", "_sprint_in");
            sprintLoop.OutputName = idleAnimation.OutputName.Replace("_idle", "_sprint_loop");
            sprintOut.OutputName = idleAnimation.OutputName.Replace("_idle", "_sprint_out");

            var superSprintIn = idleAnimation.Clone();
            var superSprintLoop = idleAnimation.Clone();
            var superSprintOut = idleAnimation.Clone();

            superSprintIn.OutputName = idleAnimation.OutputName.Replace("_idle", "_super_sprint_in");
            superSprintLoop.OutputName = idleAnimation.OutputName.Replace("_idle", "_super_sprint_loop");
            superSprintOut.OutputName = idleAnimation.OutputName.Replace("_idle", "_super_sprint_out");

            var slideIn = idleAnimation.Clone();
            var slideSprintIn = idleAnimation.Clone();
            var slideLoop = idleAnimation.Clone();
            var slideOut = idleAnimation.Clone();

            slideIn.OutputName = idleAnimation.OutputName.Replace("_idle", "_slide_in");
            slideSprintIn.OutputName = idleAnimation.OutputName.Replace("_idle", "_slide_sprint_in");
            slideLoop.OutputName = idleAnimation.OutputName.Replace("_idle", "_slide_loop");
            slideOut.OutputName = idleAnimation.OutputName.Replace("_idle", "_slide_out");

            viewModel.Animations.Add(sprintIn);
            viewModel.Animations.Add(sprintLoop);
            viewModel.Animations.Add(sprintOut);
            viewModel.Animations.Add(superSprintIn);
            viewModel.Animations.Add(superSprintLoop);
            viewModel.Animations.Add(superSprintOut);
            viewModel.Animations.Add(slideIn);
            viewModel.Animations.Add(slideSprintIn);
            viewModel.Animations.Add(slideLoop);
            viewModel.Animations.Add(slideOut);
        }
    }
}
