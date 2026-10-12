using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine.Pool;
using Xv2CoreLib;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleNodeBase : PooledEntity
    {
        public WeakReference Effect;
        public override bool IsAlive => Effect.IsAlive;
        public override EngineObjectTypeEnum EngineObjectType => EngineObjectTypeEnum.VFX;

        public ParticleSystem ParticleSystem { get; private set; }
        public ParticleNode Node;
        private ParticleNodeBase ParentNode;
        private EffectPart EffectPart;

        public int StartTime;
        public int Lifetime;
        public int Burst;
        public int BurstFrequency;
        public bool Loop;

        private float PositionX_Variance;
        private float PositionY_Variance;
        private float PositionZ_Variance;
        private float RotationX_Variance;
        private float RotationY_Variance;
        private float RotationZ_Variance;

        protected float StartRotation_Variance = 0f;
        protected float ActiveRotation_Variance = 0f;
        protected float RotationAmount = 0f;
        public SimdVector3 Velocity;
        private SimdVector3 Position;
        private SimdVector3 PreviousPositionCurve;

        public Matrix4x4 Rotation = Matrix4x4.Identity;
        /// <summary>
        /// A snapshot of the attachment bone at the time this node was created. Used for calculating AbsoluteTransform, based on EEPK flags.
        /// </summary>
        private Matrix4x4 EmitBoneTransform;

        public NodeState State { get; protected set; }
        public float CurrentFrame { get; private set; }
        public float CurrentTimeFactor { get; private set; }
        protected EffectPart SourceEffectPart => EffectPart;

        //Children Nodes
        public List<ParticleNodeBase> Nodes = new List<ParticleNodeBase>();
        public List<ParticleNodeBase> PreviousNodes = new List<ParticleNodeBase>();

        //Active child node instances
        protected List<int> ActiveInstances = new List<int>();
        protected bool ActiveInstancesUpdatedThisFrame = false;
        protected virtual bool IsRootNode => false;

        #region Initialize
        public ParticleNodeBase()
        {
            Effect = new WeakReference(null);
        }

        public ParticleNodeBase(ParticleSystem system, EffectPart effectPart)
        {
            ParticleSystem = system;
            EffectPart = effectPart;
            Node = new ParticleNode();
            Node.Burst = 1;
            Burst = 1;
        }

        public virtual void Initialize(Matrix4x4 emitPoint, SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            Effect.Target = effect;
            Velocity = velocity;
            ParticleSystem = system;
            Node = node;
            EffectPart = effectPart;
            EmitBoneTransform = system.AttachmentBone;
            CurrentTimeFactor = 0f;

            StartTime = node.StartTime + Xv2CoreLib.Random.Range(0, node.StartTime_Variance);
            Lifetime = node.Lifetime + Xv2CoreLib.Random.Range(0, node.Lifetime_Variance);
            Burst = node.Burst + Xv2CoreLib.Random.Range(0, node.Burst_Variance);
            BurstFrequency = node.BurstFrequency + Xv2CoreLib.Random.Range(0, node.BurstFrequency_Variance);
            Loop = (node.NodeFlags & NodeFlags1.Loop) == NodeFlags1.Loop;

            PositionX_Variance = Xv2CoreLib.Random.Range(0, node.Position_Variance.X);
            PositionY_Variance = Xv2CoreLib.Random.Range(0, node.Position_Variance.Y);
            PositionZ_Variance = Xv2CoreLib.Random.Range(0, node.Position_Variance.Z);
            float[] initialPosition = node.Position.Constant.Values;
            PreviousPositionCurve = new SimdVector3(initialPosition[0], initialPosition[1], initialPosition[2]);
            Position = emitPoint.Translation + PreviousPositionCurve +
                new SimdVector3(PositionX_Variance, PositionY_Variance, PositionZ_Variance);
            RotationX_Variance = Xv2CoreLib.Random.Range(0, node.Rotation_Variance.X);
            RotationY_Variance = Xv2CoreLib.Random.Range(0, node.Rotation_Variance.Y);
            RotationZ_Variance = Xv2CoreLib.Random.Range(0, node.Rotation_Variance.Z);
            UpdateTransform();
        }

        public override void ClearObjectState()
        {
            ParticleSystem = null;
            ParentNode = null;
            EffectPart = null;
            State = NodeState.Created;
            CurrentFrame = 0f;
            StartTime = 0;
            Lifetime = 0;
            Burst = 0;
            BurstFrequency = 0;
            Loop = false;
            PositionX_Variance = 0f;
            PositionY_Variance = 0f;
            PositionZ_Variance = 0f;
            RotationX_Variance = 0f;
            RotationY_Variance = 0f;
            RotationZ_Variance = 0f;

            Nodes.Clear();
            ActiveInstances.Clear();
            ActiveInstancesUpdatedThisFrame = false;
            Position = SimdVector3.Zero;
            PreviousPositionCurve = SimdVector3.Zero;
            Velocity = SimdVector3.Zero;
        }

        public virtual void Release()
        {
            ObjectPoolManager.ParticleNodeBasePool.ReleaseObject(this);
        }

        public void ReleaseAll()
        {
            lock (Nodes)
            {
                for (int i = 0; i < PreviousNodes.Count; i++)
                {
                    PreviousNodes[i].ReleaseAll();
                }

                PreviousNodes.Clear();

                for (int i = 0; i < Nodes.Count; i++)
                {
                    Nodes[i].ReleaseAll();
                }

                Nodes.Clear();
            }

            Release();
        }
        #endregion

        #region Update
        public override void Update()
        {
            StartUpdate();
            UpdateChildrenNodes();
            EndUpdate();
        }

        /// <summary>
        /// Start of the particle update loop. This should be called right at the start of the main Update() method.
        /// </summary>
        public void StartUpdate()
        {
            //Handle Start Time
            if (CurrentFrame >= StartTime && State == NodeState.Created)
            {
                State = NodeState.Active;
                CurrentFrame = 0f;
                NullEmitter();
            }

            //Handle DeactivationMode == LoopCancel.
            //Looped nodes are not valid in this state and are ended immediately, but should still follow the normal node destroy rules
            if (ParticleSystem.IsTerminating && Loop)
            {
                State = NodeState.WaitingOnChildren;
                Loop = false;

                if (Node.NodeType == ParticleNodeType.Emission)
                {
                    Emit();
                }

                return;
            }

            //Main logic loop
            if (State == NodeState.Active)
            {
                if (CurrentFrame >= Lifetime)
                {
                    if (Loop)
                    {
                        CurrentFrame = 0f;
                        NullEmitter();

                    }
                    else
                    {
                        State = NodeState.WaitingOnChildren;

                        //Emission and Null nodes only emit when they expire
                        if (Node.NodeType == ParticleNodeType.Emission)
                        {
                            Emit();
                        }

                        NullEmitter();

                        return;
                    }
                }
                CurrentTimeFactor = CurrentFrame / Lifetime;

                //Update movement
                UpdateModifiers();

                //Update position and rotation
                float[] positionValues = Node.Position.GetInterpolatedValue(CurrentTimeFactor);
                SimdVector3 positionCurve = new SimdVector3(positionValues[0], positionValues[1], positionValues[2]);
                Position += positionCurve - PreviousPositionCurve + Velocity * (ParticleSystem.CurrentFrameDelta / 60f);
                PreviousPositionCurve = positionCurve;
                UpdateTransform();
            }
        }

        private void UpdateTransform()
        {
            Transform = Matrix4x4.CreateTranslation(Position);

            if (!Node.IsEmission && (Node.NodeFlags & NodeFlags1.Unk5) != 0)
            {
                SimdVector3 direction = Velocity == SimdVector3.Zero ? SimdVector3.Zero : SimdVector3.Normalize(Velocity);
                Rotation = VfxRotation.AlignDirection(SimdVector3.UnitY, direction);
                return;
            }

            float[] rotation = Node.Rotation.GetInterpolatedValue(CurrentTimeFactor);
            Rotation = VfxRotation.Create(
                MathHelper.ToRadians(rotation[0] + RotationX_Variance),
                MathHelper.ToRadians(rotation[1] + RotationY_Variance),
                MathHelper.ToRadians(rotation[2] + RotationZ_Variance));
        }

        private void UpdateModifiers()
        {
            float deltaSeconds = ParticleSystem.CurrentFrameDelta / 60f;
            if (deltaSeconds <= 0f)
                return;

            foreach (EMP_Modifier modifier in Node.Modifiers)
            {
                switch (modifier.EmpType)
                {
                    case EMP_Modifier.EmpModifierType.Translation:
                    case EMP_Modifier.EmpModifierType.Acceleration:
                    case EMP_Modifier.EmpModifierType.AngleTranslation:
                    case EMP_Modifier.EmpModifierType.AngleAcceleration:
                    case EMP_Modifier.EmpModifierType.PointLoop:
                        {
                            if ((modifier.Flags & EMP_Modifier.ModifierFlags.Unk1) != 0)
                                break;

                            SimdVector3 axis = GetVectorValue(modifier.Axis);
                            bool isAngle = modifier.EmpType == EMP_Modifier.EmpModifierType.AngleTranslation ||
                                modifier.EmpType == EMP_Modifier.EmpModifierType.AngleAcceleration;
                            if (isAngle || modifier.EmpType == EMP_Modifier.EmpModifierType.PointLoop)
                            {
                                if ((modifier.Flags & EMP_Modifier.ModifierFlags.Unk2) != 0 && ParentNode?.State == NodeState.Active)
                                    axis += ParentNode.Position;

                                axis = isAngle ? Position - axis : axis - Position;
                            }

                            if (isAngle || modifier.EmpType == EMP_Modifier.EmpModifierType.Translation ||
                                modifier.EmpType == EMP_Modifier.EmpModifierType.Acceleration)
                            {
                                if (axis != SimdVector3.Zero)
                                    axis = SimdVector3.Normalize(axis);
                            }

                            SimdVector3 amount = axis * modifier.Factor.GetInterpolatedValue(CurrentTimeFactor);
                            if (modifier.EmpType == EMP_Modifier.EmpModifierType.Translation ||
                                modifier.EmpType == EMP_Modifier.EmpModifierType.AngleTranslation)
                            {
                                Position += amount * deltaSeconds;
                            }
                            else
                            {
                                ApplyAcceleration(amount, deltaSeconds);
                            }
                        }
                        break;
                    case EMP_Modifier.EmpModifierType.Drag:
                        Velocity *= Math.Max(0f, 1f - modifier.DragStrength.GetInterpolatedValue(CurrentTimeFactor) * deltaSeconds);
                        break;
                    case EMP_Modifier.EmpModifierType.Jitter:
                        {
                            SimdVector3 axis = GetVectorValue(modifier.Axis);
                            float strength = modifier.Factor.GetInterpolatedValue(CurrentTimeFactor) * deltaSeconds;
                            Position += new SimdVector3(
                                (Xv2CoreLib.Random.Range(0f, 1f) - 0.5f) * axis.X * strength,
                                (Xv2CoreLib.Random.Range(0f, 1f) - 0.5f) * axis.Y * strength,
                                (Xv2CoreLib.Random.Range(0f, 1f) - 0.5f) * axis.Z * strength);
                        }
                        break;
                    case EMP_Modifier.EmpModifierType.Vortex:
                        {
                            if ((modifier.Flags & EMP_Modifier.ModifierFlags.Unk1) != 0)
                                break;

                            SimdVector3 direction = GetVectorValue(modifier.Direction);
                            if (direction == SimdVector3.Zero)
                                break;

                            SimdVector3 center = GetVectorValue(modifier.Axis);
                            SimdVector3 relativePosition = Position - center;
                            if ((modifier.Flags & EMP_Modifier.ModifierFlags.Unk2) != 0 && ParentNode?.State == NodeState.Active)
                                relativePosition -= ParentNode.Position;

                            float angle = MathHelper.ToRadians(modifier.RotationRate.GetInterpolatedValue(CurrentTimeFactor) * deltaSeconds);
                            SimdVector3 rotatedPosition = SimdVector3.Transform(relativePosition,
                                Matrix4x4.CreateFromAxisAngle(SimdVector3.Normalize(direction), angle));
                            SimdVector3 radialDirection = rotatedPosition - (Position - center);
                            if (radialDirection != SimdVector3.Zero)
                                radialDirection = SimdVector3.Normalize(radialDirection);

                            Velocity += radialDirection * modifier.Radial.GetInterpolatedValue(CurrentTimeFactor) * deltaSeconds;
                            Position = center + rotatedPosition;
                        }
                        break;
                    case EMP_Modifier.EmpModifierType.Attract:
                        {
                            float remainingFrames = Lifetime - CurrentFrame;
                            if (remainingFrames < 1f)
                            {
                                Velocity *= Math.Max(0f, remainingFrames);
                                break;
                            }

                            if ((modifier.Flags & EMP_Modifier.ModifierFlags.Unk1) != 0 ||
                                ((modifier.Flags & EMP_Modifier.ModifierFlags.Unk2) != 0 && ParentNode?.State != NodeState.Active))
                                break;

                            SimdVector3 target = GetVectorValue(modifier.Axis);
                            if ((modifier.Flags & EMP_Modifier.ModifierFlags.Unk2) != 0)
                                target += ParentNode.Position;

                            float remainingSeconds = remainingFrames * deltaSeconds;
                            SimdVector3 delta = target - Position - Velocity * remainingSeconds;
                            SimdVector3 acceleration = delta * modifier.Factor.GetInterpolatedValue(CurrentTimeFactor) /
                                (0.5f * remainingSeconds * remainingSeconds);
                            ApplyAcceleration(acceleration, deltaSeconds);
                        }
                        break;
                }
            }
        }

        private SimdVector3 GetVectorValue(KeyframedVector3Value value)
        {
            float[] values = value.GetInterpolatedValue(CurrentTimeFactor);
            return new SimdVector3(values[0], values[1], values[2]);
        }

        private void ApplyAcceleration(SimdVector3 acceleration, float deltaSeconds)
        {
            Position += acceleration * (0.5f * deltaSeconds * deltaSeconds);
            Velocity += acceleration * deltaSeconds;
        }

        protected void UpdateRotation()
        {
            //For some reason, FlashOnGen (whatever that is) disables ActiveRotation

            if ((Node.NodeFlags & NodeFlags1.FlashOnGen) != NodeFlags1.FlashOnGen)
            {
                //Rotation amount is PER frame, despite what the old tooltip said

                RotationAmount += (Node.EmissionNode.ActiveRotation.GetInterpolatedValue(CurrentTimeFactor) + ActiveRotation_Variance) * ParticleSystem.CurrentFrameDelta;
            }
        }

        protected void UpdateChildrenNodes()
        {
            for (int i = PreviousNodes.Count - 1; i >= 0; i--)
            {
                PreviousNodes[i].Update();

                if (PreviousNodes[i].State == NodeState.Expired)
                {
                    PreviousNodes[i].Release();
                    PreviousNodes.RemoveAt(i);
                }
            }

            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                Nodes[i].Update();

                if (Nodes[i].State == NodeState.Expired)
                {
                    Nodes[i].Release();
                    Nodes.RemoveAt(i);
                }
            }
        }

        protected void EndUpdate()
        {
            //Handle particle lifetime
            CurrentFrame += ParticleSystem.CurrentFrameDelta;

            if (State == NodeState.WaitingOnChildren && Nodes.Count == 0 && PreviousNodes.Count == 0)
            {
                State = NodeState.Expired;
            }

            ActiveInstancesUpdatedThisFrame = false;
        }

        private void UpdateActiveInstancesCount()
        {
            if (!ActiveInstancesUpdatedThisFrame)
            {
                ActiveInstancesUpdatedThisFrame = true;

                for (int i = 0; i < Node.ChildParticleNodes.Count; i++)
                {
                    if (ActiveInstances.Count - 1 < i)
                        ActiveInstances.Add(0);

                    ActiveInstances[i] = 0;

                    for (int a = 0; a < Nodes.Count; a++)
                    {
                        if (Nodes[a].Node == Node.ChildParticleNodes[i])
                        {
                            ActiveInstances[i]++;
                        }
                    }
                }
            }
        }
        public virtual void Simulate()
        {
            StartUpdate();
            UpdateChildrenNodes();
            EndUpdate();
        }
        #endregion

        protected virtual void Emit()
        {
            UpdateActiveInstancesCount();

            for (int i = 0; i < Node.ChildParticleNodes.Count; i++)
            {
                for (int burstIndex = 0; burstIndex < Burst; burstIndex++)
                {
                    //Maximum amount of instances of this node reached, so cannot create more.
                    //If MaxInstances is 0, then there is no limit
                    if (Node.ChildParticleNodes[i].MaxInstances > 0 && Node.ChildParticleNodes[i].MaxInstances <= ActiveInstances[i] && !IsRootNode)
                        break;

                    if (IsRootNode && Node.ChildParticleNodes[i].NodeType == ParticleNodeType.Null)
                    {
                        Log.Add("ParticleSystem: Null Node type cannot be at root level. The game will crash!", LogType.Error);
                        return;
                    }

                    SimdVector3 velocity = Velocity;
                    Matrix4x4 emitTransform = GetEmitTransformationMatrix(ref velocity);
                    if (Node.IsEmitter)
                    {
                        emitTransform *= Rotation;
                        velocity = SimdVector3.TransformNormal(velocity, emitTransform);
                    }
                    emitTransform = Matrix4x4.CreateTranslation(emitTransform.Translation + Transform.Translation);

                    ParticleNodeBase newNode;
                    if (Node.ChildParticleNodes[i].NodeType == ParticleNodeType.Emitter)
                    {
                        newNode = ObjectPoolManager.GetParticleEmitter(ref emitTransform, ref velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                    }
                    else if (Node.ChildParticleNodes[i].NodeType == ParticleNodeType.Emission)
                    {
                        if (Node.ChildParticleNodes[i].EmissionNode.EmissionType == ParticleEmission.ParticleEmissionType.Plane)
                        {
                            newNode = ObjectPoolManager.GetParticlePlane(ref emitTransform, ref velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                        }
                        else if (Node.ChildParticleNodes[i].EmissionNode.EmissionType == ParticleEmission.ParticleEmissionType.Mesh)
                        {
                            newNode = ObjectPoolManager.GetParticleMesh(ref emitTransform, ref velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                            ViewportInstance.RenderSystem.AddRenderEntity(newNode);
                        }
                        else if (Node.ChildParticleNodes[i].EmissionNode.EmissionType == ParticleEmission.ParticleEmissionType.ShapeDraw)
                        {
                            newNode = new ParticleShapeDraw();
                            newNode.Initialize(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                            ViewportInstance.RenderSystem.AddRenderEntity(newNode);
                        }
                        else if (Node.ChildParticleNodes[i].EmissionNode.EmissionType == ParticleEmission.ParticleEmissionType.ConeExtrude)
                        {
                            newNode = new ParticleConeExtrude();
                            newNode.Initialize(emitTransform, velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                            ViewportInstance.RenderSystem.AddRenderEntity(newNode);
                        }
                        else
                        {
                            //placeholder
                            newNode = ObjectPoolManager.GetParticleNodeBase(ref emitTransform, ref velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                        }

                    }
                    else
                    {
                        //"null" node.
                        newNode = ObjectPoolManager.GetParticleNodeBase(ref emitTransform, ref velocity, ParticleSystem, Node.ChildParticleNodes[i], EffectPart, ParticleSystem.Effect.Target);
                    }

                    newNode.ParentNode = IsRootNode ? null : this;
                    Nodes.Add(newNode);
                    ActiveInstances[i]++;
                }
            }
        }

        protected virtual Matrix4x4 GetEmitTransformationMatrix(ref SimdVector3 velocity)
        {
            return Matrix4x4.Identity;
        }
        /// <summary>
        /// Terminate the node, according to <see cref="EffectPart.DeactivationMode"/>.
        /// </summary>
        public void Deactivate()
        {
            Loop = false;

            foreach (ParticleNodeBase node in Nodes)
            {
                node.Deactivate();
            }
        }

        protected Matrix4x4 GetAttachmentBone()
        {
            return EffectPart.InstantUpdate ? ParticleSystem.AttachmentBone : EmitBoneTransform;
        }

        private void NullEmitter()
        {
            if (Node.NodeType == ParticleNodeType.Null && !Loop)
            {
                Emit();
            }
        }

    }

    public enum NodeState : byte
    {
        /// <summary>
        /// The node has been created, but hasn't yet started.
        /// </summary>
        Created,
        /// <summary>
        /// The node is currently active.
        /// </summary>
        Active,
        /// <summary>
        /// The node has reached the end of its life, but still has active children.
        /// </summary>
        WaitingOnChildren,
        /// <summary>
        /// The node and its children are all expired.
        /// </summary>
        Expired
    }
}
