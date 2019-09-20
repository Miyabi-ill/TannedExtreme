using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using static Mono.Cecil.Cil.OpCodes;

namespace TannedExtreme
{
	public class TannedExtreme : Mod
	{
        private FieldInfo statesInfo;
        private FieldInfo rInfo;
        private FieldInfo gInfo;
        private FieldInfo bInfo;
        private FieldInfo r2Info;
        private FieldInfo g2Info;
        private FieldInfo b2Info;

        public override void Load()
        {
            var lighting = typeof(Lighting);
            statesInfo = lighting.GetField("states", BindingFlags.NonPublic | BindingFlags.Static);
            dynamic states = statesInfo.GetValue(null);
            Type lightingState = states[0][0].GetType();
            rInfo = lightingState.GetField("r");
            gInfo = lightingState.GetField("g");
            bInfo = lightingState.GetField("b");
            r2Info = lightingState.GetField("r2");
            g2Info = lightingState.GetField("g2");
            b2Info = lightingState.GetField("b2");
            IL.Terraria.Lighting.LightTiles += Lighting_LightTiles;
        }

        private void Lighting_LightTiles(ILContext il)
        {
            var c = new ILCursor(il);
            var label = c.DefineLabel();

            var lighting = typeof(Lighting);
            if (c.TryGotoNext(x => x.MatchCall(lighting, "PreRenderPhase")))
            {
                c.Index--;
                c.Remove();
                c.Emit(Ble_S, label);
                c.Index++;

                c.Emit(Ldsfld, lighting.GetField("states", BindingFlags.NonPublic | BindingFlags.Static));
                c.Index--;
                c.MarkLabel(label);
                c.Index++;
                c.Emit(Ldsfld, lighting.GetField("firstToLightX", BindingFlags.NonPublic | BindingFlags.Static));
                c.Emit(Ldsfld, lighting.GetField("lastToLightX", BindingFlags.NonPublic | BindingFlags.Static));
                c.Emit(Ldsfld, lighting.GetField("firstToLightY", BindingFlags.NonPublic | BindingFlags.Static));
                c.Emit(Ldsfld, lighting.GetField("lastToLightY", BindingFlags.NonPublic | BindingFlags.Static));
                c.EmitDelegate<Func<dynamic, int, int, int, int, bool>>(
                    delegate (dynamic states, int firstToLightX, int lastToLightX, int firstToLightY, int lastToLightY)
                    {
                        int playerCount = Main.player.Length;

                        int minX = Utils.Clamp(firstToLightX, 5, Main.maxTilesX - 1);
                        int maxX = Utils.Clamp(lastToLightX, 5, Main.maxTilesX - 1);
                        int minY = Utils.Clamp(firstToLightY, 5, Main.maxTilesY - 1);
                        int maxY = Utils.Clamp(lastToLightY, 5, Main.maxTilesY - 1);

                        for (int i = minX; i < maxX; i++)
                        {
                            dynamic array = states[i - firstToLightX];
                            for (int j = minY; j < maxY; j++)
                            {
                                object lightingState = array[j - firstToLightY];

                                float x = Main.LocalPlayer.Center.X - i * 16 - 8;
                                float y = Main.LocalPlayer.Center.Y - j * 16 - 8;
                                float distance = (float)Math.Sqrt(x * x + y * y);
                                
                                if (distance > 144)
                                {
                                    continue;
                                }

                                float brightness = Utils.Clamp(0.1f * (int)(distance / 16f), 0f, 1f);
                                if (brightness == 1f)
                                {
                                    continue;
                                }
                                float r = (float)r2Info.GetValue(lightingState) * brightness;
                                float g = (float)g2Info.GetValue(lightingState) * brightness;
                                float b = (float)b2Info.GetValue(lightingState) * brightness;
                                r2Info.SetValue(lightingState, r);
                                g2Info.SetValue(lightingState, g);
                                b2Info.SetValue(lightingState, b);
                                rInfo.SetValue(lightingState, r);
                                gInfo.SetValue(lightingState, g);
                                bInfo.SetValue(lightingState, b);
                            }
                        }
                        return true;
                    });
                c.Emit(Pop);
            }
        }
    }
}