namespace Avalonia.Platform;

/// <summary>
/// Controls the rendering backend used by the WPE WebKit adapter.
/// Note: the choice is process-global — <c>wpe_fdo_initialize_shm()</c> and
/// <c>wpe_fdo_initialize_for_egl_display()</c> are mutually exclusive.
/// Currently, the Avalonia WPE adapter only initializes SHM; EGL and DMABuf
/// modes are reserved for future use and are not yet supported.
/// </summary>
public enum WpeRenderingMode
{
    /// <summary>
    /// Automatic selection.
    /// In the current Avalonia WPE adapter this effectively behaves the same as
    /// <see cref="Shm"/>, since only SHM is implemented at this time.
    /// </summary>
    Auto = 0,

    /// <summary>Force SHM (pure software rendering, no GPU required).</summary>
    Shm,

    /// <summary>
    /// Reserved for future use: EGL (GPU render + CPU readback via glReadPixels).
    /// Not currently supported by the Avalonia WPE adapter and will result in
    /// <see cref="System.NotImplementedException"/> in the current implementation.
    /// </summary>
    Egl,

    /// <summary>
    /// Reserved for future use: DMABuf (GPU zero-copy frame export).
    /// Not currently supported by the Avalonia WPE adapter and will result in
    /// <see cref="System.NotImplementedException"/> in the current implementation.
    /// </summary>
    DmaBuf,
}
