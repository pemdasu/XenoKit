using System.Collections.Generic;
using XenoKit.Engine.Collision;

namespace XenoKit.Engine.Scripting
{
    public class Simulation : EngineObject
    {
        public readonly List<BacHitbox> ActiveHitboxes = new List<BacHitbox>();


        public override void Update()
        {
            if (!ViewportInstance.IsPlaying || !SceneManager.IsOnTab(EditorTabs.Action)) return;
            UpdateInternal();
        }

        public void Simulate()
        {
            UpdateInternal();
        }

        private void UpdateInternal()
        {
            for (int i = ActiveHitboxes.Count - 1; i >= 0; i--)
            {
                if (!ActiveHitboxes[i].IsContextValid())
                {
                    ActiveHitboxes.RemoveAt(i);
                    continue;
                }

                if (ActiveHitboxes[i].OwnerActor.Controller.FreezeActionFrames > 0)
                    continue;

                ActiveHitboxes[i].UpdateHitbox();

                //Check for collision
                foreach (Actor actor in SceneManager.Actors)
                {
                    if (actor != null)
                    {
                        if (actor.Team != ActiveHitboxes[i].Team)
                        {
                            actor.HitTest(ActiveHitboxes[i]);
                        }
                    }
                }
            }
        }
    
        
    }
}
