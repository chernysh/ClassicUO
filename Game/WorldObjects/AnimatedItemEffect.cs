﻿using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Renderer.Views;

namespace ClassicUO.Game.WorldObjects
{
    public class AnimatedItemEffect : WorldEffect
    {

        public AnimatedItemEffect(in Graphic graphic, in Hue hue, in int duration)
        {
            Graphic = graphic;
            Hue = hue;
            Duration = duration;
        }

        public AnimatedItemEffect(in WorldObject source, in Graphic graphic, in Hue hue, in int duration) : this(graphic, hue, duration)
        {
            SetSource(source);
        }

        public AnimatedItemEffect(in Serial source, in Graphic graphic, in Hue hue, in int duration) : this(source, 0, 0, 0, graphic, hue, duration)
        {

        }

        public AnimatedItemEffect(in int sourceX, in int sourceY, in int sourceZ, in Graphic graphic, in Hue hue, in int duration) : this(graphic, hue, duration)
        {
            SetSource(sourceX, sourceY, sourceZ);
        }

        public AnimatedItemEffect(in Serial sourceSerial, in int sourceX, in int sourceY, in int sourceZ,
            in Graphic graphic, in Hue hue, in int duration) : this(graphic, hue, duration)
        {
            sbyte zSrc = (sbyte) sourceZ;

            WorldObject source = World.Get(sourceSerial);
            if (source != null)
            {
                if (sourceSerial.IsMobile)
                {
                    Mobile mob = (Mobile) source;
                    if (mob != World.Player && !mob.IsMoving && (sourceX != 0 || sourceY != 0 || sourceZ != 0))
                        mob.Position = new Position((ushort) sourceX, (ushort)sourceY, zSrc);
                    SetSource(mob);
                }
                else if (sourceSerial.IsItem)
                {
                    Item item = (Item)source;
                    if (sourceX != 0 || sourceY != 0 || sourceZ != 0)
                        item.Position = new Position((ushort)sourceX, (ushort)sourceY, zSrc);
                    SetSource(item);
                }
            }
        }

        public new Graphic Graphic { get; set; }
        public int Duration { get; set; }
        public new AnimatedEffectView ViewObject => (AnimatedEffectView)base.ViewObject;



        protected override View CreateView()
        {
            return new AnimatedEffectView(this);
        }

        public override void UpdateAnimation(in double ms)
        {
            base.UpdateAnimation(in ms);
            if (LastChangeFrameTime >= Duration && Duration >= 0)
                Dispose();
            else
            {
                (int x, int y, int z) = GetSource();
                Position = new Position((ushort) x, (ushort)y, (sbyte)z);
            }
        }
    }
}