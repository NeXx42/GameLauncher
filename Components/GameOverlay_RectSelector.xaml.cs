using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GameLibary.Components
{
    /// <summary>
    /// Interaction logic for GameOverlay_RectSelector.xaml
    /// </summary>
    public partial class GameOverlay_RectSelector : Window
    {
        private bool startedDrag = false;

        public GameOverlay_RectSelector()
        {
            startedDrag = false;
            InitializeComponent();

            this.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            this.MouseMove += Canvas_MouseMove;
            this.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
        }

        private Point startPoint;
        public Rect SelectedRegion { get; private set; }


        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startedDrag = true;

            startPoint = e.GetPosition(OverlayCanvas);
            SelectionRectangle.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRectangle, startPoint.X);
            Canvas.SetTop(SelectionRectangle, startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            OverlayCanvas.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(OverlayCanvas);

                double x = Math.Min(pos.X, startPoint.X);
                double y = Math.Min(pos.Y, startPoint.Y);
                double w = Math.Abs(pos.X - startPoint.X);
                double h = Math.Abs(pos.Y - startPoint.Y);

                Canvas.SetLeft(SelectionRectangle, x);
                Canvas.SetTop(SelectionRectangle, y);
                SelectionRectangle.Width = w;
                SelectionRectangle.Height = h;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!startedDrag)
                return;

            OverlayCanvas.ReleaseMouseCapture();
            var endPoint = e.GetPosition(OverlayCanvas);

            SelectedRegion = new Rect(
                Math.Min(startPoint.X, endPoint.X),
                Math.Min(startPoint.Y, endPoint.Y),
                Math.Abs(endPoint.X - startPoint.X),
                Math.Abs(endPoint.Y - startPoint.Y)
            );

            this.DialogResult = true; // close the window
        }
    }
}
