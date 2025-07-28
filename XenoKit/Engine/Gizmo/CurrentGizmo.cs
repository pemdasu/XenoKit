using System;

namespace XenoKit.Engine.Gizmo
{
    public static class CurrentGizmo
    {
        /// <summary>
        /// Raised when the current active <see cref="GizmoBase"/> changes. This happens when the Enable method is called on any gizmo; that gizmo then becomes the active gizmo.
        /// </summary>
        public static event EventHandler CurrentGizmoChanged;
        /// <summary>
        /// Raised when the GizmoMode of <see cref="Current"/> changes
        /// </summary>
        public static event EventHandler CurrentGizmoModeChanged;

        private static GizmoBase _currentGizmo = null;

        public static GizmoBase Current => _currentGizmo;

        public static GizmoMode CurrentGizmoMode
        {
            get => _currentGizmo != null ? _currentGizmo.ActiveMode : GizmoMode.None;
            set => SetGizmoMode(value);
        }
        public static bool IsMouseOver => _currentGizmo?.IsMouseOver == true;

        public static bool AllowTranslation => _currentGizmo?.AllowTranslate == true;
        public static bool AllowRotation => _currentGizmo?.AllowRotation == true;
        public static bool AllowScale => _currentGizmo?.AllowScale == true;

        public static void SetCurrentGizmo(GizmoBase gizmo)
        {
            if (_currentGizmo != null)
                _currentGizmo.ModeChanged -= CurrentGizmo_ModeChanged;

            _currentGizmo = gizmo;
            _currentGizmo.ModeChanged += CurrentGizmo_ModeChanged;
            CurrentGizmoChanged?.Invoke(gizmo, EventArgs.Empty);
        }

        private static void CurrentGizmo_ModeChanged(object sender, EventArgs e)
        {
            CurrentGizmoChanged?.Invoke(sender, EventArgs.Empty);
        }

        public static void SetGizmoMode(GizmoMode gizmoMode)
        {
            if (_currentGizmo == null) return;

            _currentGizmo.SetGizmoMode(gizmoMode);
        }
    
        public static void Update()
        {
            _currentGizmo?.Update();
        }

        public static void Draw()
        {
            _currentGizmo?.Draw();
        }
    }
}
