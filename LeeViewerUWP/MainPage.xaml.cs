using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LeeViewerUWP {
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page {
		ObservableCollection<OperationDisplay> operations;
		ObservableCollection<Coordinates> coordinates;

        public MainPage()
        {
            this.InitializeComponent();
			var x = new SliderTooltip();
			solveDelaySlider.ThumbToolTipValueConverter = x;
			pathDelaySlider.ThumbToolTipValueConverter = x;

			if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.Xaml.FrameworkElement", "AllowFocusOnInteraction")) {
				StartButton.AllowFocusOnInteraction = true;
				SettingsButton.AllowFocusOnInteraction = true;
			}
		}

		State currentState = State.AwaitingGeneration;
		int rows, columns;
		BackgroundWorker bg, leeWorker, pathMaker;

		Cell[,] storage = null;

		Coordinates startPoint, endPoint;

		private void Cell_Clicked(Cell sender) {
			switch (currentState) {
			case State.AwaitingGeneration:
				if (sender.coordinates != startPoint && sender.coordinates != endPoint && !sender.IsObstacle)
					PointingAt.Text = String.Format("[{0}, {1}] - {2}", (sender as Cell).Row, (sender as Cell).Column, (sender as Cell).Text);
				else if (sender.coordinates == startPoint) {
					PointingAt.Text = String.Format("[{0}, {1}] - {2}", (sender as Cell).Row, (sender as Cell).Column, "Start point");
				}
				else if (sender.coordinates == endPoint) {
					PointingAt.Text = String.Format("[{0}, {1}] - {2}", (sender as Cell).Row, (sender as Cell).Column, "End point");
				}
				else {
					PointingAt.Text = String.Format("[{0}, {1}] - {2}", (sender as Cell).Row, (sender as Cell).Column, "Obstacle");
				}
				break;
			case State.AwaitingStartPoint:
				startPoint = new Coordinates() { I = sender.Row, J = sender.Column };
				sender.Background = new SolidColorBrush(Colors.LightGreen);
				sender.Foreground = new SolidColorBrush(Colors.Black);
				currentState = State.AwaitingEndPoint;
				Instructions.Text = "Select an end point";
				break;
			case State.AwaitingEndPoint:
				endPoint = new Coordinates() { I = sender.Row, J = sender.Column };
				sender.Background = new SolidColorBrush(Colors.Crimson);
				currentState = State.AwaitingObstacles;
				Instructions.Text = "Select the obstacles";
				StartButton.IsEnabled = true;
				break;
			case State.AwaitingObstacles:
				Coordinates current = new Coordinates() { I = sender.Row, J = sender.Column };
				if (!(current == startPoint || current == endPoint)) {
					sender.IsObstacle = !sender.IsObstacle;
				}
				break;
			case State.Running:
				if (sender.coordinates != startPoint && sender.coordinates != endPoint && !sender.IsObstacle)
					PointingAt.Text = String.Format("[{0}, {1}] - {2}", (sender as Cell).Row, (sender as Cell).Column, (sender as Cell).Text);
				else if (sender.coordinates == startPoint) {
					PointingAt.Text = String.Format("[{0}, {1}] - {2}", (sender as Cell).Row, (sender as Cell).Column, "Start point");
				}
				else if (sender.coordinates == endPoint) {
					PointingAt.Text = String.Format("[{0}, {1}] - {2}", (sender as Cell).Row, (sender as Cell).Column, "End point");
				}
				else {
					PointingAt.Text = String.Format("[{0}, {1}] - {2}", (sender as Cell).Row, (sender as Cell).Column, "Obstacle");
				}
				break;
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			switch (currentState) {
			case State.AwaitingGeneration:
				break;
			case State.AwaitingStartPoint:
				break;
			case State.AwaitingEndPoint:
				break;
			case State.AwaitingObstacles:
				currentState = State.Running;
				Start();
				StartButton.IsEnabled = false;
				break;
			case State.Running:
				break;
			}
		}

		#region Lee

		void Start() {
			coordinates = new ObservableCollection<Coordinates>();
			operations = new ObservableCollection<OperationDisplay>();
			stepDisplay.ItemsSource = operations;
			queueLeeDisplay.ItemsSource = coordinates;

			GC.Collect(); // Collect garbage

			operations.Insert(0, new OperationDisplay() { Icon = Symbol.Home, Text = "Marking the start point with 0." });
			storage[startPoint.I, startPoint.J].Text = "0";

			operations.Insert(0, new OperationDisplay() { Icon = Symbol.Forward, Text = "Puting the start point in the queue." });
			coordinates.Add(startPoint);

			leeWorker = new BackgroundWorker() {
				WorkerSupportsCancellation = true
			};
			leeWorker.DoWork += (object sender, DoWorkEventArgs e) => {
				while (coordinates.Count > 0) {
					if ((sender as BackgroundWorker).CancellationPending) break;
					bool delay = false;
					Coordinates current = new Coordinates();
					int waitTime = 500;
					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						switch (solveDelaySlider.Value) {
						case 0:
							delay = false;
							break;
						case 1:
							delay = true;
							waitTime = 10;
							break;
						case 2:
							delay = true;
							waitTime = 250;
							break;
						case 3:
							delay = true;
							waitTime = 1000;
							break;
						default:
							throw new NotImplementedException();
						}
						current = coordinates[0];
						operations.Insert(0, new OperationDisplay() { Icon = Symbol.Import, Text = "Extracting the current cell from the queue.\n[" + current.I + "," + current.J + "]" });
						coordinates.RemoveAt(0);
					}).AsTask().Wait();

					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						Instructions.Text = String.Format("Current cell: [{0},{1}]", current.I, current.J);
					}).AsTask().Wait();

					if (delay) Task.Delay(waitTime).Wait();

					Coordinates next;

					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						storage[current.I, current.J].Background = new SolidColorBrush(Colors.DarkBlue);
					}).AsTask().Wait();

					bool wait = false;

					// NORTH
					{
						next = new Coordinates() { I = current.I - 1, J = current.J };
						Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
							if (!isValid(next)) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go north to cell [" + next.I + "," + next.J + "].\nIt is outside the board." });
							}
							else if (storage[next.I, next.J].IsObstacle) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go north to cell [" + next.I + "," + next.J + "].\nIt is an obstacle." });
							}
							else if (!String.IsNullOrWhiteSpace(storage[next.I, next.J].Text) && int.Parse(storage[next.I, next.J].Text) <= int.Parse(storage[current.I, current.J].Text) + 1) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go north to cell [" + next.I + "," + next.J + "].\nAlready occupied." });
							}
							else {
								operations.Insert(0, new OperationDisplay() { Icon = Symbol.Up, Text = "Marking the north cell with " + (int.Parse(storage[current.I, current.J].Text) + 1) + "."});
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Forward, Text = "Puting the north cell in the queue." });
								storage[next.I, next.J].Text = (int.Parse(storage[current.I, current.J].Text) + 1).ToString();
								coordinates.Add(next);
								wait = true;
							}
						}).AsTask().Wait();
					}

					if (wait && delay) Task.Delay(waitTime).Wait();
					wait = false;

					// EAST
					{
						next = new Coordinates() { I = current.I, J = current.J + 1 };
						Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
							if (!isValid(next)) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go east to cell [" + next.I + "," + next.J + "].\nIt is outside the board." });
							}
							else if (storage[next.I, next.J].IsObstacle) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go east to cell [" + next.I + "," + next.J + "].\nIt is an obstacle." });
							}
							else if (!String.IsNullOrWhiteSpace(storage[next.I, next.J].Text) && int.Parse(storage[next.I, next.J].Text) <= int.Parse(storage[current.I, current.J].Text) + 1) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go east to cell [" + next.I + "," + next.J + "].\nAlready occupied." });
							}
							else {
								operations.Insert(0, new OperationDisplay() { Icon = Symbol.Forward, Text = "Marking the east cell with " + (int.Parse(storage[current.I, current.J].Text) + 1) + "."});
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Forward, Text = "Puting the east cell in the queue." });
								storage[next.I, next.J].Text = (int.Parse(storage[current.I, current.J].Text) + 1).ToString();
								coordinates.Add(next);
								wait = true;
							}
						}).AsTask().Wait();
					}

					if (wait && delay) Task.Delay(waitTime).Wait();
					wait = false;

					// SOUTH
					{
						next = new Coordinates() { I = current.I + 1, J = current.J };
						Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
							if (!isValid(next)) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go south to cell [" + next.I + "," + next.J + "].\nIt is outside the board." });
							}
							else if (storage[next.I, next.J].IsObstacle) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go south to cell [" + next.I + "," + next.J + "].\nIt is an obstacle." });
							}
							else if (!String.IsNullOrWhiteSpace(storage[next.I, next.J].Text) && int.Parse(storage[next.I, next.J].Text) <= int.Parse(storage[current.I, current.J].Text) + 1) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go south to cell [" + next.I + "," + next.J + "].\nAlready occupied." });
							}
							else {
								operations.Insert(0, new OperationDisplay() { Icon = Symbol.Download, Text = "Marking the south cell with " + (int.Parse(storage[current.I, current.J].Text) + 1) + "."});
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Forward, Text = "Puting the south cell in the queue." });
								storage[next.I, next.J].Text = (int.Parse(storage[current.I, current.J].Text) + 1).ToString();
								coordinates.Add(next);
								wait = true;
							}
						}).AsTask().Wait();
					}

					if (wait && delay) Task.Delay(waitTime).Wait();
					wait = false;

					// WEST
					{
						next = new Coordinates() { I = current.I, J = current.J - 1 };
						Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
							if (!isValid(next)) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go west to cell [" + next.I + "," + next.J + "].\nIt is outside the board." });
							}
							else if (storage[next.I, next.J].IsObstacle) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go west to cell [" + next.I + "," + next.J + "].\nIt is an obstacle." });
							}
							else if (!String.IsNullOrWhiteSpace(storage[next.I, next.J].Text) && int.Parse(storage[next.I, next.J].Text) <= int.Parse(storage[current.I, current.J].Text) + 1) {
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "Trying to go west to cell [" + next.I + "," + next.J + "].\nAlready occupied." });
							}
							else {
								operations.Insert(0, new OperationDisplay() { Icon = Symbol.Back, Text = "Marking the west cell with " + (int.Parse(storage[current.I, current.J].Text) + 1) + "."});
								//operations.Insert(0, new OperationDisplay() { Icon = Symbol.Forward, Text = "Puting the west cell in the queue." });
								storage[next.I, next.J].Text = (int.Parse(storage[current.I, current.J].Text) + 1).ToString();
								coordinates.Add(next);
								wait = true;
							}
						}).AsTask().Wait();
					}

					if (wait && delay) Task.Delay(waitTime).Wait();
					
					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						if (!(current == startPoint || current == endPoint)) {
							storage[current.I, current.J].Background = new SolidColorBrush(Colors.Black);
						}
						else if (current == startPoint) {
							storage[current.I, current.J].Background = new SolidColorBrush(Colors.LightGreen);
						}
						else if (current == endPoint) {
							storage[current.I, current.J].Background = new SolidColorBrush(Colors.Crimson);
						}
					}).AsTask().Wait();
				}
				return;
			};
			leeWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) => {
				pathMaker = new BackgroundWorker() {
					WorkerSupportsCancellation = true
				};
				string endText = "";
				//Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {
					endText = storage[endPoint.I, endPoint.J].Text;
				//}).AsTask().Wait();
				pathMaker.DoWork += (object sdr, DoWorkEventArgs args) => {
					if (String.IsNullOrWhiteSpace(endText)) {
						Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
							operations.Insert(0, new OperationDisplay() { Icon = Symbol.Cancel, Text = "No path found!" });
							Instructions.Text = "No path found!";
						}).AsTask().Wait();
					}
					else {
						pathFinder(endPoint);
						Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
							operations.Insert(0, new OperationDisplay() { Icon = Symbol.Accept, Text = "Path found!" });
							Instructions.Text = "Path found!";
						}).AsTask().Wait();
					}
				};
				pathMaker.RunWorkerCompleted += (object s, RunWorkerCompletedEventArgs a) => {
					StartButton.IsEnabled = true;
					StartButton.Content = "Generate";
					StartButton.Icon = new SymbolIcon(Symbol.Add);
					StartButton.Flyout = (FlyoutBase)Resources["GenerateFlyout"];
					currentState = State.AwaitingGeneration;
				};
				pathMaker.RunWorkerAsync();
			};
			leeWorker.RunWorkerAsync();
		}

		#endregion
		
		#region Generator
		private void GenerateButton_Click(object sender, RoutedEventArgs e) {
			rows = int.Parse(rowsBox.Text);
			columns = int.Parse(columnsBox.Text);
			StartButton.Content = "Start";
			StartButton.Icon = new SymbolIcon(Symbol.Play);
			StartButton.IsEnabled = false;

			bg = new BackgroundWorker() {
				WorkerSupportsCancellation = true,
				WorkerReportsProgress = true
			};
			bg.DoWork += (object sdr, DoWorkEventArgs args) => {
				int r = rows, c = columns;
				storage = new Cell[r, c];
				Viewer.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
					while (Viewer.Children.Count > 0) Viewer.Children.RemoveAt(0);
					while (Viewer.RowDefinitions.Count > 0) Viewer.RowDefinitions.RemoveAt(0);
					while (Viewer.ColumnDefinitions.Count > 0) Viewer.ColumnDefinitions.RemoveAt(0);
				}).AsTask().Wait();
				GC.Collect(); // Collect garbage
				Viewer.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
					for (int i = 0; i < r; i++) Viewer.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
					for (int j = 0; j < c; j++) Viewer.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
				}).AsTask().Wait();
				for (int i = 0; i < r; i++) {
					if ((sdr as BackgroundWorker).CancellationPending) break;
					for (int j = 0; j < c; j++) {
						if ((sdr as BackgroundWorker).CancellationPending) break;

						Viewer.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
							var cell = new Cell(i, j) {
								Background = new SolidColorBrush(Colors.Black),
								Foreground = new SolidColorBrush(Colors.White),
								Margin = new Thickness(1),
								IsTextVisible = textVisibleSwitch.IsOn
							};
							cell.Clicked += this.Cell_Clicked;
							cell.PointerEntered += (object send, PointerRoutedEventArgs ags) => {
								Cell s = send as Cell;
								if (currentState == State.Running || currentState == State.AwaitingGeneration) {
									if (s.coordinates != startPoint && s.coordinates != endPoint && !s.IsObstacle)
										PointingAt.Text = String.Format("[{0}, {1}] - {2}", (s as Cell).Row, (s as Cell).Column, (s as Cell).Text);
									else if (s.coordinates == startPoint) {
										PointingAt.Text = String.Format("[{0}, {1}] - {2}", (s as Cell).Row, (s as Cell).Column, "Start point");
									}
									else if (s.coordinates == endPoint) {
										PointingAt.Text = String.Format("[{0}, {1}] - {2}", (s as Cell).Row, (s as Cell).Column, "End point");
									}
									else {
										PointingAt.Text = String.Format("[{0}, {1}] - {2}", (s as Cell).Row, (s as Cell).Column, "Obstacle");
									}
								}
								else PointingAt.Text = String.Format("[{0}, {1}]", (s as Cell).Row, (s as Cell).Column);
							};
							cell.PointerExited += (object s, PointerRoutedEventArgs ags) => {
								PointingAt.Text = "";
							};
							Grid.SetColumn(cell, j);
							Grid.SetRow(cell, i);
							storage[i, j] = cell;
							if (!fastGenerationSwitch.IsOn) {
								Viewer.Children.Add(cell);
							}
							Instructions.Text = String.Format("Please wait... {0} cells out of {1} generated.", i * c + j + 1, r * c);
						}).AsTask().Wait();
						(sdr as BackgroundWorker).ReportProgress(Convert.ToInt32(((double)(i * c + j + 1)) / (r * c) * 100));
					}
				}
				Viewer.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
					if (fastGenerationSwitch.IsOn) {
						Instructions.Text = "Please wait... Displaying cells...";
						(sdr as BackgroundWorker).ReportProgress(100);
					}
				}).AsTask().Wait();
				Task.Delay(50).Wait();
				Viewer.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {
					if (fastGenerationSwitch.IsOn) {
						for (int i = 0; i < r; i++) {
							for (int j = 0; j < c; j++) {
								Viewer.Children.Add(storage[i, j]);
							}
						}
					}
				}).AsTask().Wait();
				GC.Collect(); // Collect garbage
			};
			bg.RunWorkerCompleted += (object sdr, RunWorkerCompletedEventArgs args) => {
				GenerateWaitDisplay.IsActive = false;
				GenerateProgressBar.Visibility = Visibility.Collapsed;
				rowsBox.IsEnabled = true;
				columnsBox.IsEnabled = true;
				GenerateButton.IsEnabled = true;
				currentState = State.AwaitingStartPoint;
				Instructions.Text = "Select a start point";
				StartButton.Flyout.Hide();
				StartButton.Flyout = null;
			};
			bg.ProgressChanged += (object sdr, ProgressChangedEventArgs args) => {
				if (args.ProgressPercentage != 100) {
					GenerateProgressBar.IsIndeterminate = false;
					GenerateProgressBar.Value = args.ProgressPercentage;
				}
				else {
					GenerateProgressBar.IsIndeterminate = true;
				}
			};
			GenerateWaitDisplay.IsActive = true;
			GenerateProgressBar.Visibility = Visibility.Visible;
			rowsBox.IsEnabled = false;
			columnsBox.IsEnabled = false;
			GenerateButton.IsEnabled = false;
			Instructions.Text = "Please wait...";
			bg.RunWorkerAsync();
		}

#endregion

		private void ToggleButton_Click(object sender, RoutedEventArgs e) {
			if((sender as AppBarToggleButton).IsChecked.Value) {
				leeActionsColumn.Width = new GridLength(1, GridUnitType.Star);
				leeStackColumn.Width = new GridLength(1, GridUnitType.Auto);
				leeStackColumn.MinWidth = 10;
				(sender as AppBarToggleButton).Content = "Hide details pane";
				(sender as AppBarToggleButton).Icon = new SymbolIcon(Symbol.Forward);
			}
			else {
				leeActionsColumn.Width = new GridLength(0, GridUnitType.Pixel);
				leeStackColumn.MinWidth = 0;
				leeStackColumn.Width = new GridLength(0, GridUnitType.Pixel);
				(sender as AppBarToggleButton).Content = "Show details pane";
				(sender as AppBarToggleButton).Icon = new SymbolIcon(Symbol.Back);
			}
		}

		private void fastGenerationSwitch_Toggled(object sender, RoutedEventArgs e) {
			if ((sender as ToggleSwitch).IsOn) {
				fastGenerationWarning.Visibility = Visibility.Visible;
			}
			else {
				fastGenerationWarning.Visibility = Visibility.Collapsed;
			}
		}

		private void Box_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args) {
			if (!String.IsNullOrWhiteSpace(sender.Text) && int.TryParse(rowsBox.Text, out int rows) && int.TryParse(columnsBox.Text, out int columns)) {
				if (rows * columns > 100) {
					solveDelaySlider.Maximum = 1;
					if (solveDelaySlider.Value > 1) solveDelaySlider.Value = 1;
				}
				else if (rows * columns > 25) {
					solveDelaySlider.Maximum = 2;
					if (solveDelaySlider.Value > 1) solveDelaySlider.Value = 2;
				}
				else {
					solveDelaySlider.Maximum = 3;
				}
				GenerateButton.IsEnabled = true;
			}
			else GenerateButton.IsEnabled = false;
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e) {
			try {
				bg?.CancelAsync();
				leeWorker?.CancelAsync();
				pathMaker?.CancelAsync();
				while (Viewer.Children.Count > 0) Viewer.Children.RemoveAt(0);
				while (Viewer.RowDefinitions.Count > 0) Viewer.RowDefinitions.RemoveAt(0);
				while (Viewer.ColumnDefinitions.Count > 0) Viewer.ColumnDefinitions.RemoveAt(0);
				
				StartButton.IsEnabled = true;
				StartButton.Content = "Generate";
				StartButton.Icon = new SymbolIcon(Symbol.Add);
				StartButton.Flyout = (FlyoutBase)Resources["GenerateFlyout"];
				currentState = State.AwaitingGeneration;
				Instructions.Text = "Please generate the board.";
			}
			catch (Exception) {

			}
		}

		private bool isValid(Coordinates coord) {
			if (coord.I < 0) return false;
			if (coord.J < 0) return false;
			if (coord.I >= rows) return false;
			if (coord.J >= columns) return false;
			return true;
		}

		#region Path finder

		private void pathFinder(Coordinates current) {
			int currentNo = 0;
			bool delay = true;
			int waitTime = 0;
			Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
				currentNo = int.Parse(storage[current.I, current.J].Text);
			}).AsTask().Wait();

			if (current == startPoint) return;

			Coordinates next;
			string nextText= "";
			bool nextObstacle = false;

			bool exit = false;

			// NORTH
			next = new Coordinates() { I = current.I - 1, J = current.J };
			if (isValid(next)) {
				Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
					nextText = storage[next.I, next.J].Text;
					nextObstacle = storage[next.I, next.J].IsObstacle;
				}).AsTask().Wait();
			}
			{
				if (isValid(next) && !nextObstacle && !String.IsNullOrWhiteSpace(nextText) && currentNo - 1 == int.Parse(nextText)) {
					pathFinder(next);
					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						switch (pathDelaySlider.Value) {
						case 0:
							delay = false;
							break;
						case 1:
							delay = true;
							waitTime = 10;
							break;
						case 2:
							delay = true;
							waitTime = 250;
							break;
						case 3:
							delay = true;
							waitTime = 1000;
							break;
						default:
							throw new NotImplementedException();
						}
					}).AsTask().Wait();
					if (delay) Task.Delay(waitTime).Wait();
					exit = true;
					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						if (!(current == startPoint || current == endPoint)) {
							storage[current.I, current.J].Background = new SolidColorBrush(Colors.DarkViolet);
						}
					}).AsTask().Wait();
				}
			}
			if (exit) return;
			// EAST
			next = new Coordinates() { I = current.I, J = current.J + 1 };
			if (isValid(next)) {
				Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
					nextText = storage[next.I, next.J].Text;
					nextObstacle = storage[next.I, next.J].IsObstacle;
				}).AsTask().Wait();
			}
			{
				if (isValid(next) && !nextObstacle && !String.IsNullOrWhiteSpace(nextText) && currentNo - 1 == int.Parse(nextText)) {
					pathFinder(next);

					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						switch (pathDelaySlider.Value) {
						case 0:
							delay = false;
							break;
						case 1:
							delay = true;
							waitTime = 10;
							break;
						case 2:
							delay = true;
							waitTime = 250;
							break;
						case 3:
							delay = true;
							waitTime = 1000;
							break;
						default:
							throw new NotImplementedException();
						}
					}).AsTask().Wait();
					if (delay) Task.Delay(waitTime).Wait();
					exit = true;
					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						if (!(current == startPoint || current == endPoint)) {
							storage[current.I, current.J].Background = new SolidColorBrush(Colors.DarkViolet);
						}
					}).AsTask().Wait();
				}
			}
			if (exit) return;
			// SOUTH
			next = new Coordinates() { I = current.I + 1, J = current.J };
			if (isValid(next)) {
				Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
					nextText = storage[next.I, next.J].Text;
					nextObstacle = storage[next.I, next.J].IsObstacle;
				}).AsTask().Wait();
			}
			{
				if (isValid(next) && !nextObstacle && !String.IsNullOrWhiteSpace(nextText) && currentNo - 1 == int.Parse(nextText)) {
					pathFinder(next);

					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						switch (pathDelaySlider.Value) {
						case 0:
							delay = false;
							break;
						case 1:
							delay = true;
							waitTime = 10;
							break;
						case 2:
							delay = true;
							waitTime = 250;
							break;
						case 3:
							delay = true;
							waitTime = 1000;
							break;
						default:
							throw new NotImplementedException();
						}
					}).AsTask().Wait();
					if (delay) Task.Delay(waitTime).Wait();
					exit = true;
					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						if (!(current == startPoint || current == endPoint)) {
							storage[current.I, current.J].Background = new SolidColorBrush(Colors.DarkViolet);
						}
					}).AsTask().Wait();
				}
			}
			if (exit) return;
			// WEST
			next = new Coordinates() { I = current.I, J = current.J - 1 };
			if (isValid(next)) {
				Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
					nextText = storage[next.I, next.J].Text;
					nextObstacle = storage[next.I, next.J].IsObstacle;
				}).AsTask().Wait();
			}
			{
				if (isValid(next) && !nextObstacle && !String.IsNullOrWhiteSpace(nextText) && currentNo - 1 == int.Parse(nextText)) {
					pathFinder(next);

					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						switch (pathDelaySlider.Value) {
						case 0:
							delay = false;
							break;
						case 1:
							delay = true;
							waitTime = 10;
							break;
						case 2:
							delay = true;
							waitTime = 250;
							break;
						case 3:
							delay = true;
							waitTime = 1000;
							break;
						default:
							throw new NotImplementedException();
						}
					}).AsTask().Wait();
					if (delay) Task.Delay(waitTime).Wait();
					Instructions.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
						if (!(current == startPoint || current == endPoint)) {
							storage[current.I, current.J].Background = new SolidColorBrush(Colors.DarkViolet);
						}
					}).AsTask().Wait();
				}
			}
		}
#endregion
	}
}
