using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace LeeViewerUWP {
	/// <summary>
	/// Interaction logic for Cell.xaml
	/// </summary>
	public partial class Cell : UserControl {
		public Cell() {
			InitializeComponent();
			Tapped += Cell_Clicked;
		}

		public Cell(int i, int j) {
			InitializeComponent();
			Row = i;
			Column = j;
			Tapped += Cell_Clicked;
		}

		void setTooltip() {
			ToolTip x = new ToolTip();
			if (IsObstacle) x.Content = "Obstacle";
			else if (isTextVisible) x.Content = TextDisplay.Text;
			else x.Content = hiddenText;
			Viewer.SetValue(ToolTipService.ToolTipProperty, x);
		}

		public delegate void CellEventArgs(Cell sender);
		public event CellEventArgs Clicked;

		private void Cell_Clicked(object sender, TappedRoutedEventArgs e) => Clicked?.Invoke(this);

		public int Row { get; set; }
		public int Column { get; set; }

		public new Brush Background { set => Viewer.Background = value; }
		public new Brush Foreground { set => TextDisplay.Foreground = value; }
		string hiddenText = "";
		public string Text { get => isTextVisible ? TextDisplay.Text : hiddenText; set { if (isTextVisible) TextDisplay.Text = value; else hiddenText = value; setTooltip(); } }

		private bool _IsObstacle = false;
		public bool IsObstacle { get => _IsObstacle; set {
				_IsObstacle = value;
				if (_IsObstacle) {
					Background = new SolidColorBrush(Colors.Yellow);
					Foreground = new SolidColorBrush(Colors.Black);
				}
				else {
					Background = new SolidColorBrush(Colors.Black);
					Foreground = new SolidColorBrush(Colors.White);
				}
			} }

		public Coordinates coordinates { get => new Coordinates() { I = Row, J = Column }; }

		private bool isTextVisible = true;
		public bool IsTextVisible { get => isTextVisible; set => isTextVisible = value; }
	}
}
