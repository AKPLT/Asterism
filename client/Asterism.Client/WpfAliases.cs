// UseWindowsForms が追加する global using System.Windows.Forms / System.Drawing と
// WPF 型の衝突を解消するエイリアス
global using Application = System.Windows.Application;
global using MessageBox = System.Windows.MessageBox;
global using UserControl = System.Windows.Controls.UserControl;
global using Color = System.Windows.Media.Color;
global using ColorConverter = System.Windows.Media.ColorConverter;
global using Brushes = System.Windows.Media.Brushes;
global using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
