using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace CosmoWhisper.Services
{
    public class AnimationService
    {
        private Storyboard? GetStoryboard(string key)
        {
            try { return System.Windows.Application.Current?.FindResource(key) as Storyboard; }
            catch { return null; }
        }

        public void StartIdleAnimations(FrameworkElement target)
        {
            // Stop high-frequency render loops while idle to guarantee 0.0% CPU usage
            GetStoryboard("IdleBreathingAnimation")?.Stop(target);
            GetStoryboard("IridescentAnimation")?.Stop(target);
            GetStoryboard("SatelliteAnimation")?.Stop(target);
            GetStoryboard("JiggleAnimation")?.Stop(target);
        }

        public void StartRecordingAnimations(FrameworkElement target)
        {
            GetStoryboard("JiggleAnimation")?.Begin(target);
        }

        public void StartThinkingAnimations(FrameworkElement target)
        {
            GetStoryboard("ThinkingDotsAnimation")?.Begin(target);
            GetStoryboard("ThinkingAnimation")?.Begin(target);
        }

        public void StopThinkingAnimations(FrameworkElement target)
        {
            GetStoryboard("ThinkingDotsAnimation")?.Stop(target);
            GetStoryboard("ThinkingAnimation")?.Stop(target);
        }

        public void BeginRipple(FrameworkElement target)
        {
            GetStoryboard("RippleAnimation")?.Begin(target);
        }

        public void SetSatelliteSpeed(FrameworkElement target, double speed)
        {
            GetStoryboard("SatelliteAnimation")?.SetSpeedRatio(target, speed);
        }

        public void DrawSineWave(Canvas canvas, Path path, Path glow, double phase, double amplitudeNorm)
        {
            const double canvasWidth = 120;
            const double centerY = 22;
            const double minAmp = 2.0;
            const double maxAmp = 18.0;
            double amplitude = minAmp + (amplitudeNorm * (maxAmp - minAmp));
            const int points = 80;

            var figure = new System.Windows.Media.PathFigure { IsClosed = false, IsFilled = false };
            for (int i = 0; i <= points; i++)
            {
                double x = (canvasWidth / points) * i;
                double y = centerY + amplitude * System.Math.Sin(phase + (i / (double)points) * System.Math.PI * 4);
                if (i == 0) figure.StartPoint = new System.Windows.Point(x, y);
                else figure.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(x, y), true));
            }

            var geometry = new System.Windows.Media.PathGeometry();
            geometry.Figures.Add(figure);
            path.Data = geometry;
            glow.Data = geometry;
        }
    }
}