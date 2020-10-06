using System;

namespace LunaServer
{
    using EndlessOnline.Communication;
    using EndlessOnline.Replies;

    public class MapNPC
    {
        public Map Map { get; }
        public int Id { get; }
        public int X { get; internal set; }
        public int Y { get; internal set; }
        public int Index { get; }
        public int TotalHP { get; }
        public int HP { get; private set; }
        public bool Alive { get; private set; }
        public Direction Direction { get; internal set; }

        public MapNPC(Map map, int id, int x, int y, int index, int totalHP, int hp)
        {
            this.Map = map;
            this.Id = id;
            this.X = x;
            this.Y = y;
            this.Index = index;
            this.Alive = false;
            this.Direction = Direction.Down;
            this.TotalHP = totalHP;
            this.HP = hp;
        }

        internal void Damage(Character from, int amount)
        {
            from.LastNPCAttacked = this;

            this.HP -= amount;

            if (this.HP > 0)
            {
                var builder = new Packet(PacketFamily.NPC, PacketAction.Reply);
                builder.AddShort(from.Session.PlayerId);
                builder.AddChar((byte)from.Direction);
                builder.AddShort((ushort)this.Index);
                builder.AddThree(amount);
                builder.AddShort((ushort)Math.Clamp((double)(this.HP / (double)this.TotalHP * 100.0), 0, 100));
                builder.AddChar(1); // TODO: Add spell support.

                foreach (var character in this.Map.Characters)
                {
                    if (character.InRange(this))
                        character.Session.Send(builder);
                }
            }
            else
            {
                this.Killed(from, amount);
            }
        }

        internal void Killed(Character from, int amount)
        {
            this.Alive = false;

            var builder = new Packet(PacketFamily.NPC, PacketAction.Spec);
            builder.AddShort(from.Session.PlayerId);
            builder.AddChar((byte)from.Direction);
            builder.AddShort((ushort)this.Index);
            builder.AddShort(0); // drop uid
            builder.AddShort(0); // drop id
            builder.AddChar((byte)this.X);
            builder.AddChar((byte)this.Y);
            builder.AddInt(0); // drop amount
            builder.AddThree(amount); // damage
            builder.AddInt(from.Exp);

            foreach (var character in this.Map.Characters)
            {
                if (character.InRange(this))
                    character.Session.Send(builder);
            }
        }

        internal void Walk(Direction direction)
        {
            this.Map.Walk(this, direction);
        }

        internal void Spawn()
        {
            if (this.Alive)
                return;

            this.Alive = true;

            var builder = new Packet(PacketFamily.Appear, PacketAction.Reply);
            builder.AddChar(0);
            builder.AddByte(255);
            builder.AddChar((byte)this.Index);
            builder.AddShort((ushort)this.Id);
            builder.AddChar((byte)this.X);
            builder.AddChar((byte)this.Y);
            builder.AddChar((byte)this.Direction);

            foreach (var character in this.Map.Characters)
                if (character.InRange(this))
                    character.Session.Send(builder);
        }

        internal void RemoveFromView(Character character)
        {
            var builder = new Packet(PacketFamily.NPC, PacketAction.Player);
            builder.AddChar((byte)this.Index);

            if (character.X > 200 && character.Y > 200)
            {
                builder.AddChar(0); // x
                builder.AddChar(0); // y
            }
            else
            {
                builder.AddChar(252); // x
                builder.AddChar(252); // y
            }

            builder.AddChar((byte)this.Direction); // direction
            builder.AddByte(255);
            builder.AddByte(255);
            builder.AddByte(255);

            var builder2 = new Packet(PacketFamily.NPC, PacketAction.Spec);
            builder2.AddShort(0); // killer pid
            builder2.AddChar(0); // killer direction
            builder2.AddShort((ushort)this.Index);

            character.Session.Send(builder);
            character.Session.Send(builder2);
        }
    }
}