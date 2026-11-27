using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using static Stillpoint.SpiritArt;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        SkyState sky;
        float skyCheckT = -99, meteorNext = 3;
        readonly List<Meteor> meteors = new List<Meteor>();
        readonly HashSet<string> told = new HashSet<string>();

        float EclipseDark => Smooth(0.5f, 1, sky.SunCover) * 0.8f;

        string CometName => "Comet " + (string.IsNullOrEmpty(state.Name) ? "Pebble" : state.Name);

        void StepSky(float t, float dt)
        {
            if (t - skyCheckT > 1)
            {
                skyCheckT = t;
                var now = Clock();
                sky = SkyEvents.At(now, SkyEvents.PlaceFor(state.Lat, state.Lon, now), state.Kp);
                AnnounceSky(t);
            }
            float dark = Smooth(0.55f, 0.9f, nightK) * (1 - cloudK) * (1 - fogK);
            if (dark > 0.05f && t > meteorNext)
            {
                float moon = Sky.MoonLight(Sky.MoonPhase(Clock()));
                float perMinute = 0.9f + 30 * sky.ShowerLevel * (1 - 0.4f * moon);
                SpawnMeteor(t);
                meteorNext = t + (float)(-Math.Log(1 - Rand * 0.999) * 60 / (perMinute * dark));
            }
            meteors.RemoveAll(m => t - m.T0 > m.Life + 2);
        }

        void SpawnMeteor(float t)
        {
            MeteorSeen(t);
            var shower = sky.Shower != null && Rand < sky.ShowerLevel / (sky.ShowerLevel + 0.03f) ? sky.Shower : null;
            float x = 30 + Rand * 240, y = 14 + Rand * 100, dx, dy;
            if (shower != null)
            {
                dx = x - shower.Radiant.X;
                dy = y - shower.Radiant.Y;
            }
            else
            {
                float a = 0.3f + Rand * 0.5f;
                dx = Cos(a) * (Rand < 0.5f ? -1 : 1);
                dy = Sin(a);
            }
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            meteors.Add(new Meteor
            {
                T0 = t,
                X = x,
                Y = y,
                Dx = dx / length,
                Dy = dy / length,
                Speed = 170 + Rand * 140,
                Life = 0.35f + Rand * 0.45f,
                Length = 16 + Rand * 26,
                Bright = 0.55f + Rand * 0.45f,
                Fireball = Rand < 0.04f,
            });
        }

        void DrawSky(Graphics g, float t, float night)
        {
            float clear = (1 - cloudK) * (1 - fogK);
            if (meteors.Count == 0 && sky.Aurora <= 0 && sky.Comet <= 0) return;
            var state0 = g.Save();
            using (var disc = Disc()) g.SetClip(disc, CombineMode.Intersect);
            if (sky.Aurora > 0) SkyArt.DrawAurora(g, t, sky.Aurora * night * clear);
            if (sky.Comet > 0)
            {
                float u = sky.CometProgress;
                SkyArt.DrawComet(g, 80 + 110 * u, 54 - 16 * Sin(u * Pi), sky.Comet * night * clear, t);
            }
            foreach (var m in meteors) SkyArt.DrawMeteor(g, m, t);
            g.Restore(state0);
        }

        void AnnounceSky(float t)
        {
            var now = Clock();
            string night = SkyEvents.NightOf(now).ToString("yyyy-MM-dd");
            bool visible = nightK > 0.6f && cloudK < 0.7f;
            if (visible && sky.ShowerLevel > 0.25f && Tell("meteors" + night, Visitors.Meteors)) Chat(Voice.Meteors(sky.Shower, Tone), 3.8f);
            if (visible && sky.Aurora > 0.2f && Tell("aurora" + night, Visitors.Aurora))
            {
                Chat(Voice.Aurora(sky.AuroraReal), 4.2f);
                zoomT = t + 0.5f;
            }
            if (visible && sky.Comet > 0.2f && Tell("comet" + night, Visitors.Comet))
                Chat(sky.CometProgress < 0.1f ? Voice.Comet(CometName, Tone) : $"{CometName} is still here. Look at that tail.", 3.8f);
            var e = sky.Eclipse;
            if (e == null) return;
            string key = "eclipse" + e.Greatest.ToString("yyyyMMddHH");
            bool active = sky.SunCover > 0.15f || sky.MoonShadow > 0.1f || sky.MoonDim > 0.2f;
            if (sky.EclipseHere && active && Tell(key, Visitors.Eclipse))
            {
                Chat(Voice.Eclipse(e, true), 4.4f);
                zoomT = t + 0.5f;
            }
            else if (!sky.EclipseHere && Tell(key + "far", null)) Chat(Voice.Eclipse(e, false), 3.8f);
        }

        bool Tell(string key, string visitor)
        {
            if (told.Contains(key) || visitor != null && state.SeenToday.Contains(visitor)) return false;
            told.Add(key);
            if (visitor != null) Visit(visitor);
            return true;
        }
    }
}
