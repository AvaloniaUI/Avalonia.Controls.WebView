using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

static class WpfExtensions
{
	public static Brush ToBrush (this Color color) => new SolidColorBrush (color);		

	public static T AddChildren<T> (this T uiElement, params object[] children) where T : IAddChild
	{
		foreach (var child in children)
			if (child != null)
				uiElement.AddChild (child);

		return uiElement;
	}

	public static ItemCollection AddItems (this ItemCollection items, params object[] children)
	{
		foreach (var child in children)
			items.Add (child);

		return items;
	}

	public static T AddColumns<T> (this T gridView, params System.Windows.Controls.GridViewColumn[] columns) where T : System.Windows.Controls.GridView
	{
		foreach (var c in columns)
			gridView.Columns.Add (c);

		return gridView;
	}

	public static T AddHandler<T> (this T item, RoutedEvent routedEvent, Action<RoutedEventArgs> handler) where T : UIElement
	{
		item.AddHandler (routedEvent, new RoutedEventHandler ((sender, args) => handler (args)));
		return item;
	}

	public static T SetDock<T> (this T element, Dock dock) where T : UIElement
	{
		if (element == null) throw new ArgumentNullException ("element");
		element.SetValue (DockPanel.DockProperty, dock);
		return element;
	}

	public static T AddRowDefinitions<T> (this T grid, int rowCount) where T : Grid
	{
		for (int i = 0; i < rowCount; i++)
			grid.RowDefinitions.Add (new RowDefinition ());

		return grid;
	}

	public static T AddRowDefinitions<T> (this T grid, params RowDefinition[] rows) where T : Grid
	{
		foreach (var row in rows)
			grid.RowDefinitions.Add (row ?? new RowDefinition ());

		return grid;
	}

	public static T AddColumnDefinitions<T> (this T grid, int columnCount) where T : Grid
	{
		for (int i = 0; i < columnCount; i++)
			grid.ColumnDefinitions.Add (new ColumnDefinition ());

		return grid;
	}

	public static T AddColumnDefinitions<T> (this T grid, params ColumnDefinition[] cols) where T : Grid
	{
		foreach (var col in cols)
			grid.ColumnDefinitions.Add (col ?? new ColumnDefinition ());

		return grid;
	}

	public static T SetRowColumn<T> (this T element, int row, int column) where T : UIElement
	{
		if (element == null) throw new ArgumentNullException ("element");
		element.SetValue (Grid.RowProperty, row);
		element.SetValue (Grid.ColumnProperty, column);
		return element;
	}

	public static T SetVisible<T> (this T uiElement, bool visible) where T : UIElement
	{
		uiElement.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
		return uiElement;
	}

	public static TextBlock ToTextBlock (this string text) => new TextBlock { Text = text };

	public static T BindToResource<T> (this T element, DependencyProperty prop, object name) where T : FrameworkElement
	{
		element.SetResourceReference (prop, name);
		return element;
	}
}
