using Xv2CoreLib.EEPK;
using Xv2CoreLib.ECF;
using LB_Common.Numbers;
using System.Collections.Generic;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Vfx.Asset
{
    public class VfxColorFade : VfxAsset
    {
        private readonly ECF_File EcfFile;

        private List<VfxColorFadeEntry> Values;

        public VfxColorFade(ECF_File ecfFile, EffectPart effectPart, Actor actor, bool spawnedByProjectile = false) : base(System.Numerics.Matrix4x4.Identity, effectPart, actor, spawnedByProjectile)
        {
            EcfFile = ecfFile;
            Values = new List<VfxColorFadeEntry>(ecfFile.Nodes.Count);
            SetDefaultValues();

            if (EcfFile.Nodes.Count == 0)
                IsFinished = true;
        }

        private void SetDefaultValues()
        {
            for(int i = 0; i < EcfFile.Nodes.Count; i++)
            {
                if (Values.Count <= i)
                    Values.Add(new VfxColorFadeEntry());

                Values[i].Multi[0] = 1;
                Values[i].Multi[1] = 1;
                Values[i].Multi[2] = 1;
                Values[i].Multi[3] = 1;
                Values[i].RimColor[0] = 0;
                Values[i].RimColor[1] = 0;
                Values[i].RimColor[2] = 0;
                Values[i].RimColor[3] = 0;
                Values[i].AddColor[0] = 0;
                Values[i].AddColor[1] = 0;
                Values[i].AddColor[2] = 0;
                Values[i].AddColor[3] = 0;
            }
        }

        public override void Update()
        {
            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation)
            {
                SetDefaultValues();
                return;
            }

            base.Update();
            Update(false);
        }

        public override void Simulate()
        {
            Update(true);
        }

        private void Update(bool simulate)
        {
            if (!HasStarted) return;

            int active = 0;

            for (int i = 0; i < EcfFile.Nodes.Count; i++)
            {
                if (Values.Count <= i)
                    Values.Add(new VfxColorFadeEntry());

                if (Values[i].IsFinished) continue;

                ECF_Node node = EcfFile.Nodes[i];
                float localTime = Values[i].Time - node.StartTime;

                // EndTime is a duration that starts at StartTime.
                if (localTime >= node.EndTime)
                {
                    if (node.Loop && !IsTerminating && node.EndTime > 0)
                    {
                        localTime %= node.EndTime;
                        Values[i].Time = node.StartTime + localTime;
                    }
                    else
                    {
                        Values[i].IsFinished = true;
                        Values[i].IsLive = false;
                        continue;
                    }
                }

                active++;
                Values[i].IsLive = localTime >= 0f;

                if (Values[i].IsLive && !simulate)
                {
                    float time = GetKeyframeTime(localTime, node.EndTime);

                    float[] multi = node.MultiColor.GetInterpolatedValue(time);
                    float[] rim = node.RimColor.GetInterpolatedValue(time);
                    float[] add = node.AddColor.GetInterpolatedValue(time);
                    float multi_A = node.MultiColor_Transparency.GetInterpolatedValue(time);
                    float rim_A = node.RimColor_Transparency.GetInterpolatedValue(time);
                    float add_A = node.AddColor_Transparency.GetInterpolatedValue(time);

                    Values[i].Multi[0] = multi[0] * multi_A;
                    Values[i].Multi[1] = multi[1] * multi_A;
                    Values[i].Multi[2] = multi[2] * multi_A;
                    Values[i].Multi[3] = node.BlendingFactor.GetInterpolatedValue(time);
                    Values[i].RimColor[0] = rim[0] * rim_A;
                    Values[i].RimColor[1] = rim[1] * rim_A;
                    Values[i].RimColor[2] = rim[2] * rim_A;
                    Values[i].RimColor[3] = rim_A * rim_A;
                    Values[i].AddColor[0] = add[0] * add_A;
                    Values[i].AddColor[1] = add[1] * add_A;
                    Values[i].AddColor[2] = add[2] * add_A;
                    Values[i].AddColor[3] = add_A * add_A;
                }

                if (simulate)
                {
                    Values[i].Time += 1f;
                }
                else if (ViewportInstance.IsPlaying)
                {
                    Values[i].Time += FrameStep;
                }
            }

            if (active == 0)
            {
                IsFinished = true;
            }
        }

        // Keyframe times are stored as 0 to 1 over the node duration.
        private static float GetKeyframeTime(float localTime, int duration)
        {
            return duration > 1 ? localTime / (duration - 1) : localTime;
        }

        public VfxColorFadeEntry GetColorFadeEntry(string material)
        {
            for(int i = 0; i < EcfFile.Nodes.Count; i++)
            {
                if (Values.Count <= i || !Values[i].IsLive) continue;

                // The node material is a case-sensitive pattern that must contain the drawn material name.
                if(!EcfFile.Nodes[i].UseMaterial || EcfFile.Nodes[i].Material.Contains(material))
                {
                    return Values[i];
                }
            }

            return null;
        }

        public override void SeekNextFrame()
        {
            base.SeekNextFrame();
            
            foreach(VfxColorFadeEntry node in Values)
            {
                node.Time++;
            }
        }

        public override void SeekPrevFrame()
        {
            base.SeekPrevFrame();

            for(int i = 0; i < Values.Count; i++)
            {
                if (Values[i].Time > 0)
                    Values[i].Time--;
                else if (EcfFile.Nodes[i].Loop)
                    Values[i].Time = EcfFile.Nodes[i].EndTime;
            }
        }
    }

    public class VfxColorFadeEntry
    {
        public float Time = 0f;
        public bool IsFinished = false;
        public bool IsLive = false;
        public float[] Multi = new float[4];
        public float[] RimColor = new float[4];
        public float[] AddColor = new float[4];
    }
}
