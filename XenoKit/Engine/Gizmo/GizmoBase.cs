using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Xv2CoreLib;
using XenoKit.Engine.Gizmo.TransformOperations;
using XenoKit.Engine.Shapes;
using Plane = Microsoft.Xna.Framework.Plane;
using XenoKit.Editor;

namespace XenoKit.Engine.Gizmo
{
    public enum GizmoAxis
    {
        None,
        X,
        Z,
        Y,
        XY,
        ZY,
        ZX,
        YZ,
        XYZ
    }

    public enum GizmoMode
    {
        Translate = 0,
        Rotate = 1,
        Scale = 2,
        None = 3
    }

    public abstract class GizmoBase : EngineObject
    {
        public event EventHandler ModeChanged;
        public bool IsVisible => ActiveMode != GizmoMode.None;
        private bool WasAutoHidden { get; set; }
        protected virtual Matrix WorldMatrix { get; } = Matrix.Identity;

        //State
        protected GizmoAxis ActiveAxis = GizmoAxis.None;
        public GizmoMode ActiveMode { get; protected set; } = GizmoMode.Translate;

        #region Stuff
        private readonly Sphere GeometryXYZ;

        // -- Lines (Vertices) -- //
        protected VertexPositionColor[] _translationLineVertices;
        protected const float LINE_LENGTH = 3f;
        protected const float LINE_OFFSET = 1f;

        // -- Quads -- //
        protected Quad[] _quads;
        protected readonly BasicEffect _quadEffect;

        // -- Colors -- //
        protected Color[] _axisColors;
        protected Color _highlightColor;

        //Effects
        protected BasicEffect _lineEffect;
        protected BasicEffect _meshEffect;
        protected BasicEffect _geometryXyzEffect;

        // -- Screen Scale -- //
        protected Matrix _screenScaleMatrix;
        protected float _screenScale;

        // -- Position - Rotation -- //
        protected Vector3 _position = Vector3.Zero;
        protected Matrix _rotationMatrix = Matrix.Identity;

        protected Vector3 _localForward = Vector3.Forward;
        protected Vector3 _localUp = Vector3.Up;
        protected Vector3 _localRight;

        // -- Matrices -- //
        protected Matrix _objectOrientedWorld;
        protected Matrix _axisAlignedWorld;
        protected Matrix[] _modelLocalSpace;
        protected Matrix _gizmoWorld = Matrix.Identity;

        // -- Translation Variables -- //
        protected Vector3 _tDelta;
        protected Vector3 _lastIntersectionPosition;
        protected Vector3 _intersectPosition;

        public bool PrecisionModeEnabled;
        protected const float PRECISION_MODE_SCALE = 0.1f;

        #endregion

        #region BoundingBoxes

        protected const float MULTI_AXIS_THICKNESS = 0.05f;
        protected const float SINGLE_AXIS_THICKNESS = 0.2f;

        protected static BoundingBox XAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(LINE_OFFSET, 0, 0),
                                       new Vector3(LINE_OFFSET + LINE_LENGTH, SINGLE_AXIS_THICKNESS, SINGLE_AXIS_THICKNESS));
            }
        }

        protected static BoundingBox YAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(0, LINE_OFFSET, 0),
                                       new Vector3(SINGLE_AXIS_THICKNESS, LINE_OFFSET + LINE_LENGTH, SINGLE_AXIS_THICKNESS));
            }
        }

        protected static BoundingBox ZAxisBox
        {
            get
            {
                return new BoundingBox(new Vector3(0, 0, LINE_OFFSET),
                                       new Vector3(SINGLE_AXIS_THICKNESS, SINGLE_AXIS_THICKNESS, LINE_OFFSET + LINE_LENGTH));
            }
        }

        protected static BoundingBox XZAxisBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(LINE_OFFSET, MULTI_AXIS_THICKNESS, LINE_OFFSET));
            }
        }

        protected BoundingBox XYBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(LINE_OFFSET, LINE_OFFSET, MULTI_AXIS_THICKNESS));
            }
        }

        protected BoundingBox YZBox
        {
            get
            {
                return new BoundingBox(Vector3.Zero,
                                       new Vector3(MULTI_AXIS_THICKNESS, LINE_OFFSET, LINE_OFFSET));
            }
        }

        #endregion

        #region BoundingSpheres

        protected const float RADIUS = 1f;
        protected const float XYZ_RADIUS = 0.5f;

        protected BoundingSphere XSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[1].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }
        protected BoundingSphere YSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[7].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }
        protected BoundingSphere ZSphere
        {
            get
            {
                return new BoundingSphere(Vector3.Transform(_translationLineVertices[13].Position, _gizmoWorld),
                                          RADIUS * _screenScale);
            }
        }

        protected BoundingSphere XYZSphere
        {
            get => new BoundingSphere(Vector3.Transform(Vector3.Zero, _gizmoWorld), XYZ_RADIUS * _screenScale);
        }
        #endregion

        protected virtual ITransformOperation TransformOperation { get; set; }
        protected Action CallbackOnBegin { get; set; }
        protected Action CallbackOnComplete { get; set; }

        //Settings
        protected virtual bool AutoPause => false;
        public virtual bool AllowTranslate => true;
        public virtual bool AllowRotation => true;
        public virtual bool AllowScale => true;
        protected virtual bool LocalTranslate => false;

        public bool IsMouseOver { get; private set; }

        public GizmoBase()
        {
            GeometryXYZ = new Sphere(XYZ_RADIUS * 2f, true);

            _geometryXyzEffect = new BasicEffect(GraphicsDevice) { VertexColorEnabled = true };
            _lineEffect = new BasicEffect(GraphicsDevice) { VertexColorEnabled = true, AmbientLightColor = Vector3.One, EmissiveColor = Vector3.One };
            _meshEffect = new BasicEffect(GraphicsDevice);
            _quadEffect = new BasicEffect(GraphicsDevice) { World = Matrix.Identity, DiffuseColor = _highlightColor.ToVector3(), Alpha = 0.5f };
            _quadEffect.EnableDefaultLighting();

            _modelLocalSpace = new Matrix[3];
            _modelLocalSpace[0] = Matrix.CreateWorld(new Vector3(LINE_LENGTH, 0, 0), Vector3.Left, Vector3.Up);
            _modelLocalSpace[1] = Matrix.CreateWorld(new Vector3(0, LINE_LENGTH, 0), Vector3.Down, Vector3.Left);
            _modelLocalSpace[2] = Matrix.CreateWorld(new Vector3(0, 0, LINE_LENGTH), Vector3.Forward, Vector3.Up);

            // -- Colors: X,Y,Z,Highlight -- //
            _axisColors = new Color[3];
            _axisColors[0] = Color.Red;
            _axisColors[1] = Color.Green;
            _axisColors[2] = Color.Blue;
            _highlightColor = Color.Gold;

            #region Fill Axis-Line array
            const float halfLineOffset = LINE_OFFSET / 2;
            var vertexList = new List<VertexPositionColor>(18);

            // helper to apply colors
            Color xColor = _axisColors[0];
            Color yColor = _axisColors[1];
            Color zColor = _axisColors[2];

            // -- X Axis -- // index 0 - 5
            vertexList.Add(new VertexPositionColor(new Vector3(halfLineOffset, 0, 0), xColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_LENGTH, 0, 0), xColor));

            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, 0), xColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, LINE_OFFSET, 0), xColor));

            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, 0), xColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, LINE_OFFSET), xColor));

            // -- Y Axis -- // index 6 - 11
            vertexList.Add(new VertexPositionColor(new Vector3(0, halfLineOffset, 0), yColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_LENGTH, 0), yColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, 0), yColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, LINE_OFFSET, 0), yColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, 0), yColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, LINE_OFFSET), yColor));

            // -- Z Axis -- // index 12 - 17
            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, halfLineOffset), zColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, LINE_LENGTH), zColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, LINE_OFFSET), zColor));
            vertexList.Add(new VertexPositionColor(new Vector3(LINE_OFFSET, 0, LINE_OFFSET), zColor));

            vertexList.Add(new VertexPositionColor(new Vector3(0, 0, LINE_OFFSET), zColor));
            vertexList.Add(new VertexPositionColor(new Vector3(0, LINE_OFFSET, LINE_OFFSET), zColor));

            // -- Convert to array -- //
            _translationLineVertices = vertexList.ToArray();

            #endregion


            #region Translucent Quads
            _quads = new Shapes.Quad[3];
            _quads[0] = new Shapes.Quad(new Vector3(halfLineOffset, halfLineOffset, 0), Vector3.Backward, Vector3.Up, LINE_OFFSET,
                                 LINE_OFFSET); //XY
            _quads[1] = new Shapes.Quad(new Vector3(halfLineOffset, 0, halfLineOffset), Vector3.Up, Vector3.Right, LINE_OFFSET,
                                 LINE_OFFSET); //XZ
            _quads[2] = new Shapes.Quad(new Vector3(0, halfLineOffset, halfLineOffset), Vector3.Right, Vector3.Up, LINE_OFFSET,
                                 LINE_OFFSET); //ZY 
            #endregion

            if (!AllowTranslate && ActiveMode == GizmoMode.Translate)
                ActiveMode = GizmoMode.Rotate;

            if (!AllowRotation && ActiveMode == GizmoMode.Rotate)
                ActiveMode = GizmoMode.Scale;

            if (!AllowTranslate && !AllowRotation && !AllowScale)
                throw new ArgumentException("GizmoBase: Translate, Rotate and Scale are all not allowed on derived class.");
        }

        public void SetCallback(Action callbackOnBegin, Action callbackOnComplete)
        {
            CallbackOnBegin = callbackOnBegin;
            CallbackOnComplete = callbackOnComplete;
        }

        public void Enable()
        {
            CurrentGizmo.SetCurrentGizmo(this);

            if (!IsVisible && IsContextValid())
            {
                if (AllowTranslate)
                    ActiveMode = GizmoMode.Translate;
                else if (AllowRotation)
                    ActiveMode = GizmoMode.Rotate;
                else if (AllowScale)
                    ActiveMode = GizmoMode.Scale;
                else
                    throw new ArgumentException("GizmoBase.Enable: no GizmoMode is possible");
            }

            WasAutoHidden = false;
        }

        public void Disable()
        {
            CancelOperation();

            WasAutoHidden = false;
            ActiveMode = GizmoMode.None;
            ModeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CancelOperation()
        {
            if (TransformOperation != null)
            {
                if (!TransformOperation.IsFinished)
                    TransformOperation.Cancel();

                TransformOperation = null;
            }

            Input.ClearDragEvent(MouseButtons.Left, this);
        }

        public void SetGizmoMode(GizmoMode mode)
        {
            if (mode == ActiveMode) return;

            CancelOperation();
            ActiveMode = mode;
            WasAutoHidden = IsVisible ? false : WasAutoHidden;
            ModeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetContext()
        {
            Enable();
        }

        public virtual bool IsContextValid()
        {
            return false;
        }

        public override void Draw()
        {
            if (!IsVisible || ActiveMode == GizmoMode.None || ViewportInstance.IsFullScreen) return;

            //Meshes
            for (int i = 0; i < 3; i++) //(order: x, y, z)
            {
                GizmoGeometry activeModel;
                switch (ActiveMode)
                {
                    case GizmoMode.Translate:
                        activeModel = Geometry.Translate;
                        break;
                    case GizmoMode.Rotate:
                        activeModel = Geometry.Rotate;
                        break;
                    default:
                        activeModel = Geometry.Scale;
                        break;
                }

                _meshEffect.World = _modelLocalSpace[i] * _gizmoWorld;
                _meshEffect.View = ViewportInstance.Camera.ViewMatrix;
                _meshEffect.Projection = ViewportInstance.Camera.ProjectionMatrix;

                _meshEffect.DiffuseColor = _axisColors[i].ToVector3();
                _meshEffect.EmissiveColor = _axisColors[i].ToVector3();

                _meshEffect.CurrentTechnique.Passes[0].Apply();

                foreach (var pass in _meshEffect.CurrentTechnique.Passes)
                {
                    GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                    GraphicsDevice.BlendState = BlendState.Opaque;
                    GraphicsDevice.DepthStencilState = DepthStencilState.None;
                    pass.Apply();

                    GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                        activeModel.Vertices, 0, activeModel.Vertices.Length,
                        activeModel.Indices, 0, activeModel.Indices.Length / 3);
                }


            }

            //Lines
            _lineEffect.World = _gizmoWorld;
            _lineEffect.View = ViewportInstance.Camera.ViewMatrix;
            _lineEffect.Projection = ViewportInstance.Camera.ProjectionMatrix;

            foreach (var pass in _lineEffect.CurrentTechnique.Passes)
            {
                GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = DepthStencilState.None;
                pass.Apply();

                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _translationLineVertices, 0,
                                             _translationLineVertices.Length / 2);
            }

            //Quads
            switch (ActiveMode)
            {
                case GizmoMode.Scale:
                case GizmoMode.Translate:
                    switch (ActiveAxis)
                    {
                        #region Draw Quads
                        case GizmoAxis.ZX:
                        case GizmoAxis.YZ:
                        case GizmoAxis.XY:
                            {
                                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                                GraphicsDevice.RasterizerState = RasterizerState.CullNone;

                                _quadEffect.World = _gizmoWorld;
                                _quadEffect.View = ViewportInstance.Camera.ViewMatrix;
                                _quadEffect.Projection = ViewportInstance.Camera.ProjectionMatrix;

                                _quadEffect.CurrentTechnique.Passes[0].Apply();

                                Shapes.Quad activeQuad = new Shapes.Quad();
                                switch (ActiveAxis)
                                {
                                    case GizmoAxis.XY:
                                        activeQuad = _quads[0];
                                        break;
                                    case GizmoAxis.ZX:
                                        activeQuad = _quads[1];
                                        break;
                                    case GizmoAxis.YZ:
                                        activeQuad = _quads[2];
                                        break;
                                }

                                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                                                                    activeQuad.Vertices, 0, 4,
                                                                    activeQuad.Indexes, 0, 2);

                                GraphicsDevice.BlendState = BlendState.Opaque;
                                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                            }
                            break;
                            #endregion
                    }
                    break;
            }
        
            //XYZ Sphere (Scale)
            if(ActiveMode == GizmoMode.Scale && AllowScale)
            {
                GeometryXYZ.Draw(_gizmoWorld, Viewport.Instance.Camera.ViewMatrix, Viewport.Instance.Camera.ProjectionMatrix, ActiveAxis == GizmoAxis.XYZ ? Color.Yellow : Color.White);
            }
        }
        
        public override void Update()
        {
            IsMouseOver = false;
            if (ViewportInstance.IsFullScreen) return;

            if (IsVisible && (Input.IsKeyDown(Keys.U) || Input.IsKeyDown(Keys.Escape)))
            {
                Disable();
            }

            if (!IsContextValid() && IsVisible)
            {
                Disable();
                WasAutoHidden = true;
            }

            if (!IsVisible)
            {
                GizmoMode tryGizmoMode = GetGizmoMode();

                //Attempt to enable control if valid input
                if (tryGizmoMode != GizmoMode.None || (WasAutoHidden && IsContextValid()))
                {
                    ActiveMode = tryGizmoMode;
                    ModeChanged?.Invoke(this, EventArgs.Empty);
                    Enable();
                }
            }

            if (!IsVisible || ActiveMode == GizmoMode.None) return;

            UpdateElements();
            UpdateMouse();

            _lastIntersectionPosition = _intersectPosition;

            if (Input.HasDragEvent(MouseButtons.Left, this) && ActiveAxis != GizmoAxis.None && TransformOperation != null && ((!ViewportInstance.IsPlaying && AutoPause) || !AutoPause))
            {
                switch (ActiveMode)
                {
                    case GizmoMode.Scale:
                    case GizmoMode.Translate:
                        {
                            Vector3 delta = Vector3.Zero;
                            Ray ray = EngineUtils.CalculateRay(Input.MousePosition);

                            Matrix transform = Matrix.Invert(_rotationMatrix);
                            ray.Position = Vector3.Transform(ray.Position, transform);
                            ray.Direction = Vector3.TransformNormal(ray.Direction, transform);


                            switch (ActiveAxis)
                            {
                                case GizmoAxis.XY:
                                case GizmoAxis.X:
                                    {
                                        Plane plane = new Plane(Vector3.Forward,
                                                                Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).Z);

                                        float? intersection = ray.Intersects(plane);
                                        if (intersection.HasValue)
                                        {
                                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                                            if (_lastIntersectionPosition != Vector3.Zero)
                                            {
                                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                                            }
                                            delta = ActiveAxis == GizmoAxis.X
                                                        ? new Vector3(_tDelta.X, 0, 0)
                                                        : new Vector3(_tDelta.X, _tDelta.Y, 0);
                                        }
                                    }
                                    break;
                                case GizmoAxis.Z:
                                case GizmoAxis.YZ:
                                case GizmoAxis.Y:
                                    {
                                        Plane plane = new Plane(Vector3.Left, Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).X);

                                        float? intersection = ray.Intersects(plane);
                                        if (intersection.HasValue)
                                        {
                                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                                            if (_lastIntersectionPosition != Vector3.Zero)
                                            {
                                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                                            }
                                            switch (ActiveAxis)
                                            {
                                                case GizmoAxis.Y:
                                                    delta = new Vector3(0, _tDelta.Y, 0);
                                                    break;
                                                case GizmoAxis.Z:
                                                    delta = new Vector3(0, 0, _tDelta.Z);
                                                    break;
                                                default:
                                                    delta = new Vector3(0, _tDelta.Y, _tDelta.Z);
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                                case GizmoAxis.ZX:
                                    {
                                        Plane plane = new Plane(Vector3.Down, Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).Y);

                                        float? intersection = ray.Intersects(plane);
                                        if (intersection.HasValue)
                                        {
                                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                                            if (_lastIntersectionPosition != Vector3.Zero)
                                            {
                                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                                            }
                                        }
                                        delta = new Vector3(_tDelta.X, 0, _tDelta.Z);
                                    }
                                    break;
                                case GizmoAxis.XYZ:
                                    {
                                        //Uniform scale mode
                                        Plane plane = new Plane(Vector3.Forward,
                                                                Vector3.Transform(_position, Matrix.Invert(_rotationMatrix)).Z);

                                        float? intersection = ray.Intersects(plane);
                                        if (intersection.HasValue)
                                        {
                                            _intersectPosition = (ray.Position + (ray.Direction * intersection.Value));
                                            if (_lastIntersectionPosition != Vector3.Zero)
                                            {
                                                _tDelta = _intersectPosition - _lastIntersectionPosition;
                                            }
                                            delta = new Vector3(_tDelta.X, _tDelta.X, _tDelta.X);
                                        }
                                    }
                                    break;
                            }

                            if (PrecisionModeEnabled)
                                delta *= PRECISION_MODE_SCALE;

                            //delta.X = -delta.X; //Axis correction

                            if (ActiveMode == GizmoMode.Translate)
                            {
                                // transform (local or world)
                                if(!LocalTranslate)
                                    delta = Vector3.Transform(delta, _rotationMatrix);

                                TransformOperation?.UpdatePos(delta);
                            }
                            else if (ActiveMode == GizmoMode.Scale)
                            {
                                // -- Apply Scale -- //
                                TransformOperation?.UpdateScale(delta);
                            }
                        }
                        break;
                    case GizmoMode.Rotate:
                        {
                            float delta = (Input.MousePosition.X - Input.PreviousMouseState.X);
                            delta *= (float)Viewport.Instance.GameTime.ElapsedGameTime.TotalSeconds;

                            if (PrecisionModeEnabled)
                                delta *= PRECISION_MODE_SCALE;

                            if(TransformOperation?.RotationType == RotationType.Quaternion)
                            {
                                Quaternion rotAmount = TransformOperation.GetRotation();

                                switch (ActiveAxis)
                                {
                                    case GizmoAxis.X:
                                        rotAmount *= Quaternion.CreateFromAxisAngle(Matrix.Identity.Left, delta);
                                        break;
                                    case GizmoAxis.Y:
                                        rotAmount *= Quaternion.CreateFromAxisAngle(Matrix.Identity.Up, delta);
                                        break;
                                    case GizmoAxis.Z:
                                        rotAmount *= Quaternion.CreateFromAxisAngle(Matrix.Identity.Forward, delta);
                                        break;
                                }

                                //Update animation
                                TransformOperation?.UpdateRot(rotAmount);
                            }
                            else if(TransformOperation?.RotationType == RotationType.EulerAngles)
                            {
                                Vector3 rotAmount = TransformOperation.GetRotationAngles();

                                switch (ActiveAxis)
                                {
                                    case GizmoAxis.X:
                                        rotAmount.X += 50f * delta;
                                        break;
                                    case GizmoAxis.Y:
                                        rotAmount.Y += 50f * delta;
                                        break;
                                    case GizmoAxis.Z:
                                        rotAmount.Z += 50f * delta;
                                        break;
                                }

                                rotAmount.ClampEuler();
                                /*
                                if (rotAmount.X > 360)
                                    rotAmount.X -= 360;
                                if (rotAmount.Y > 360)
                                    rotAmount.Y -= 360;
                                if (rotAmount.Z > 360)
                                    rotAmount.Z -= 360;

                                if (rotAmount.X < 0f)
                                    rotAmount.X += 360;
                                if (rotAmount.Y < 0f)
                                    rotAmount.Y += 360;
                                if (rotAmount.Z < 0f)
                                    rotAmount.Z += 360;
                                */

                                //Update animation
                                TransformOperation?.UpdateRot(rotAmount);
                            }
                        }
                        break;
                }
            }
            else
            {
                var tryGizmoMode = GetGizmoMode();

                if (tryGizmoMode != GizmoMode.None)
                {
                    ActiveMode = tryGizmoMode;
                    ModeChanged?.Invoke(this, EventArgs.Empty);
                }

                if (Input.MouseState.LeftButton == ButtonState.Released && Input.MouseState.RightButton == ButtonState.Released)
                    SelectAxis(Input.MousePosition);
            }

            // -- Reset Colors to default -- //
            ApplyColor(GizmoAxis.X, _axisColors[0]);
            ApplyColor(GizmoAxis.Y, _axisColors[1]);
            ApplyColor(GizmoAxis.Z, _axisColors[2]);

            // -- Apply Highlight -- //
            ApplyColor(ActiveAxis, _highlightColor);
        }

        private bool CanHaveTextInput()
        {
            if (!IsVisible || ViewportInstance.IsFullScreen) return false;
            return (ActiveAxis == GizmoAxis.X || ActiveAxis == GizmoAxis.Y || ActiveAxis == GizmoAxis.Z) ||
                (ActiveAxis == GizmoAxis.XYZ && ActiveMode == GizmoMode.Scale);
        }

        protected virtual void UpdateMouse()
        {
            if (Input.MouseState.LeftButton == ButtonState.Pressed && !Input.HasDragEvent(MouseButtons.Left) && ActiveAxis != GizmoAxis.None)
            {
                if (ViewportInstance.IsPlaying && AutoPause)
                {
                    ViewportInstance.IsPlaying = false;
                }

                Input.RegisterDragEvent(MouseButtons.Left, this);
                ResetDeltas();
                CallbackOnBegin?.Invoke();
                StartTransformOperation();
            }

            if ((Input.MouseState.LeftButton == ButtonState.Released && Input.HasDragEvent(MouseButtons.Left, this)) || ((ViewportInstance.IsPlaying && AutoPause) && Input.HasDragEvent(MouseButtons.Left, this)))
            {
                Input.ClearDragEvent(MouseButtons.Left);

                if (TransformOperation?.Modified == true)
                {
                    TransformOperation.Confirm();
                    OnConfirm();
                }
                else if (TransformOperation != null)
                {
                    TransformOperation.Cancel();
                    OnCancel();
                }

                CallbackOnComplete?.Invoke();
                TransformOperation = null;
                ResetDeltas();
            }
        }

        protected abstract void StartTransformOperation();

        protected void ResetDeltas()
        {
            _tDelta = Vector3.Zero;
            _lastIntersectionPosition = Vector3.Zero;
            _intersectPosition = Vector3.Zero;
        }

        protected void UpdateElements()
        {
            _position = new Vector3(WorldMatrix.Translation.X, WorldMatrix.Translation.Y, WorldMatrix.Translation.Z);

            Vector3 vLength = ViewportInstance.Camera.CameraState.Position - _position;

            //Force elements to have a min size
            const float minDistance = 0.5f;
            if (Vector3.Distance(ViewportInstance.Camera.CameraState.Position, _position) < minDistance)
            {
                //vLength = new Vector3(0, 0, minDistance);
            }

            const float scaleFactor = 25.0f;

            _screenScale = vLength.Length() / scaleFactor;
            _screenScaleMatrix = Matrix.CreateScale(new Vector3(_screenScale));

            _localForward = WorldMatrix.Forward;
            _localUp = WorldMatrix.Up;
            // -- Vector Rotation (Local/World) -- //
            _localForward.Normalize();
            _localRight = Vector3.Cross(_localForward, _localUp);
            _localUp = Vector3.Cross(_localRight, _localForward);
            _localRight.Normalize();
            _localUp.Normalize();

            // -- Create Both World Matrices -- //
            _objectOrientedWorld = _screenScaleMatrix * Matrix.CreateWorld(_position, _localForward, _localUp);
            //_axisAlignedWorld = _screenScaleMatrix * Matrix.CreateWorld(_position, Matrix.Identity.Forward, Matrix.Identity.Up);

            //Asign world (local)
            _gizmoWorld = _objectOrientedWorld;
            _rotationMatrix.Forward = _localForward;
            _rotationMatrix.Up = _localUp;
            _rotationMatrix.Right = _localRight;
        }

        protected GizmoMode GetGizmoMode()
        {
            if (Input.IsKeyDown(Keys.T) && AllowTranslate)
            {
                return GizmoMode.Translate;
            }
            else if (Input.IsKeyDown(Keys.R) && AllowRotation)
            {
                return GizmoMode.Rotate;
            }
            else if (Input.IsKeyDown(Keys.G) && AllowScale)
            {
                return GizmoMode.Scale;
            }
            return GizmoMode.None;
        }

        /// <summary>
        /// Helper method for applying color to the gizmo lines.
        /// </summary>
        protected void ApplyColor(GizmoAxis axis, Color color)
        {
            switch (ActiveMode)
            {
                case GizmoMode.Scale:
                case GizmoMode.Translate:
                    switch (axis)
                    {
                        case GizmoAxis.X:
                            ApplyLineColor(0, 6, color);
                            break;
                        case GizmoAxis.Y:
                            ApplyLineColor(6, 6, color);
                            break;
                        case GizmoAxis.Z:
                            ApplyLineColor(12, 6, color);
                            break;
                        case GizmoAxis.XY:
                            ApplyLineColor(0, 4, color);
                            ApplyLineColor(6, 4, color);
                            break;
                        case GizmoAxis.YZ:
                            ApplyLineColor(6, 2, color);
                            ApplyLineColor(12, 2, color);
                            ApplyLineColor(10, 2, color);
                            ApplyLineColor(16, 2, color);
                            break;
                        case GizmoAxis.ZX:
                            ApplyLineColor(0, 2, color);
                            ApplyLineColor(4, 2, color);
                            ApplyLineColor(12, 4, color);
                            break;
                    }
                    break;
                case GizmoMode.Rotate:
                    switch (axis)
                    {
                        case GizmoAxis.X:
                            ApplyLineColor(0, 6, color);
                            break;
                        case GizmoAxis.Y:
                            ApplyLineColor(6, 6, color);
                            break;
                        case GizmoAxis.Z:
                            ApplyLineColor(12, 6, color);
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Apply color on the lines associated with translation mode (re-used in Scale)
        /// </summary>
        private void ApplyLineColor(int startindex, int count, Color color)
        {
            for (int i = startindex; i < (startindex + count); i++)
            {
                _translationLineVertices[i].Color = color;
            }
        }

        /// <summary>
        /// Per-frame check to see if mouse is hovering over any axis.
        /// </summary>
        protected void SelectAxis(Vector2 mousePosition)
        {
            if (!IsVisible) return;

            float closestintersection = float.MaxValue;
            Ray ray = EngineUtils.CalculateRay(mousePosition);

            float? intersection;

            //Check for uniform scale hit before the ray is transformed into local space
            if (ActiveMode == GizmoMode.Scale)
            {
                intersection = XYZSphere.Intersects(ray);

                if (intersection.HasValue)
                {
                    if (intersection.Value < closestintersection)
                    {
                        ActiveAxis = GizmoAxis.XYZ;
                        IsMouseOver = true;
                        closestintersection = intersection.Value;

                        //If there is any hit at all, simply return. This takes priority over the other axis modes
                        return;
                    }
                }
            }

            if (ActiveMode == GizmoMode.Translate || ActiveMode == GizmoMode.Scale)
            {
                // transform ray into local-space of the boundingboxes.
                ray.Direction = Vector3.TransformNormal(ray.Direction, Matrix.Invert(_gizmoWorld));
                ray.Position = Vector3.Transform(ray.Position, Matrix.Invert(_gizmoWorld));
            }

            #region X,Y,Z Boxes
            intersection = XAxisBox.Intersects(ray);
            if (intersection.HasValue)
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.X;
                    closestintersection = intersection.Value;
                }
            intersection = YAxisBox.Intersects(ray);
            if (intersection.HasValue)
            {
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.Y;
                    closestintersection = intersection.Value;
                }
            }
            intersection = ZAxisBox.Intersects(ray);
            if (intersection.HasValue)
            {
                if (intersection.Value < closestintersection)
                {
                    ActiveAxis = GizmoAxis.Z;
                    closestintersection = intersection.Value;
                }
            }
            #endregion

            if (ActiveMode == GizmoMode.Rotate)
            {
                #region BoundingSpheres

                intersection = XSphere.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value < closestintersection)
                    {
                        ActiveAxis = GizmoAxis.X;
                        closestintersection = intersection.Value;
                    }
                intersection = YSphere.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value < closestintersection)
                    {
                        ActiveAxis = GizmoAxis.Y;
                        closestintersection = intersection.Value;
                    }
                intersection = ZSphere.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value < closestintersection)
                    {
                        ActiveAxis = GizmoAxis.Z;
                        closestintersection = intersection.Value;
                    }

                #endregion
            }
            if (ActiveMode == GizmoMode.Translate || ActiveMode == GizmoMode.Scale)
            {
                // if no axis was hit (x,y,z) set value to lowest possible to select the 'farthest' intersection for the XY,XZ,YZ boxes. 
                // This is done so you may still select multi-axis if you're looking at the gizmo from behind!
                if (closestintersection >= float.MaxValue)
                    closestintersection = float.MinValue;

                #region BoundingBoxes
                intersection = XYBox.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value > closestintersection)
                    {
                        ActiveAxis = GizmoAxis.XY;
                        closestintersection = intersection.Value;
                    }
                intersection = XZAxisBox.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value > closestintersection)
                    {
                        ActiveAxis = GizmoAxis.ZX;
                        closestintersection = intersection.Value;
                    }
                intersection = YZBox.Intersects(ray);
                if (intersection.HasValue)
                    if (intersection.Value > closestintersection)
                    {
                        ActiveAxis = GizmoAxis.YZ;
                        closestintersection = intersection.Value;
                    }
                #endregion
            }
            if (closestintersection >= float.MaxValue || closestintersection <= float.MinValue)
            {
                ActiveAxis = GizmoAxis.None;
                IsMouseOver = false;
            }
            else
            {
                IsMouseOver = true;
            }

        }

        public virtual bool IsEnabledOnBone(int bone)
        {
            return false;
        }
    
        public virtual void OnConfirm()
        {

        }

        public virtual void OnCancel()
        {

        }
    }
}
