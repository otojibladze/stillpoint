using System;
using System.Drawing;

namespace Stillpoint
{
    sealed partial class SpiritRing
    {
        float habitatNext, arrivalsAt;

        Env LiveEnv()
        {
            var now = Clock();
            var real = state.Weather != null && state.Weather.Fresh(now) ? state.Weather : null;
            var part = Habitats.PartAt(now, state.Sun);
            bool falling = rainK > 0.2f;
            return new Env
            {
                Month = Garden.SeasonMonth(now),
                Part = part,
                Temp = real != null && !float.IsNaN(real.Temp) ? real.Temp : Habitats.TypicalTemp(now, part),
                Wind = windK,
                Raining = falling && !snowing,
                SnowFalling = falling && snowing,
                SnowGround = snowGround,
                Wet = WetGround || falling && !snowing,
            };
        }

        bool Fits(string id) => Now >= arrivalsAt && Habitats.Fits(id, LiveEnv());

        void StepHabitat(float t)
        {
            if (t < habitatNext) return;
            habitatNext = t + 0.5f;
            var e = LiveEnv();
            bool Off(string id) => !Habitats.Fits(id, e);
            if ((bird.State == 1 || bird.State == 2) && Off(Visitors.Bird)) SendAway(bird, t);
            if ((butterfly.State == 1 || butterfly.State == 2) && Off(Visitors.Butterfly)) SendAway(butterfly, t);
            if (dragonfly.Active && Off(Visitors.Dragonfly)) DragonflyLeave(t);
            if ((bee.State == 1 || bee.State == 2) && Off(Visitors.Bee)) BeeLeave(bee, t);
            if (ladybug.State >= 1 && ladybug.State <= 3 && Off(Visitors.Ladybug)) TakeOff(ladybug, t);
            if (squirrel.State == 1 && Off(Visitors.Squirrel)) SquirrelRun(squirrel, t);
            if ((rabbit.State == 1 || rabbit.State == 2) && Off(Visitors.Rabbit)) LeaveRabbit(t);
            if (cat.D == 1 && cat.Active && (cat.State <= 3 || cat.State == 6) && Off(Visitors.Cat)) LeaveCat(t);
            if (owl.State == 2 && Off(Visitors.Owl)) OwlLeave(owl, t);
            if (moth.State == 2 && Off(Visitors.Moth)) MothFlee(t, new PointF(SX, Ground));
            if (mouse.Active && Off(Visitors.Mouse)) MouseFlee(t);
            if (worm.Active && Off(Visitors.Worm)) WormRetract(t);
            if ((frog.State == 1 || frog.State == 2) && Off(Visitors.Frog)) { frog.State = 3; frog.T0 = t; frog.K = frog.A = 0; }
        }

        void StartPondFrog(float t)
        {
            if (frog.Active || !Fits(Visitors.Frog)) return;
            frog.State = 1;
            frog.T0 = t;
            frog.D = 0;
            frog.K = frog.A = 0;
        }

        void DragonflyLeave(float t)
        {
            float since = t - dragonfly.T0;
            if (since < 7) dragonfly.T0 = t - 7;
        }
    }
}
