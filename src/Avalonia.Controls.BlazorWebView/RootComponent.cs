using Microsoft.AspNetCore.Components;

namespace Avalonia.Controls.BlazorWebView;

/// <summary>
/// Describes a root Blazor component hosted in a <see cref="BlazorWebView"/>.
/// </summary>
public sealed class RootComponent
{
    /// <summary>
    /// CSS selector for the element that hosts the component (for example <c>#app</c>).
    /// </summary>
    public string Selector { get; set; } = "#app";

    /// <summary>
    /// The component type implementing <see cref="IComponent"/>.
    /// </summary>
    public Type? ComponentType { get; set; }

    /// <summary>
    /// Optional parameters for the root component.
    /// </summary>
    public IDictionary<string, object?>? Parameters { get; set; }

    internal ParameterView ToParameterView()
    {
        if (Parameters is null || Parameters.Count == 0)
            return ParameterView.Empty;
        return ParameterView.FromDictionary(Parameters);
    }
}
