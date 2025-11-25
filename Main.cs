using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace NekoBuffUniqueItemV3
{
    [ApiVersion(2, 1)]
    public class Main : TerrariaPlugin
    {
        public override string Name => "NekoBuffUniqueItemV3";
        public override string Author => "NyaruToru";
        public override string Description => "Buff per item instance using prefix UID, vanilla tooltip update.";
        public override Version Version => new Version(1, 0, 0);

        // UID (prefix) → custom damage
        private readonly Dictionary<int, int> _itemDamage = new();

        public Main(Terraria.Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("nekobuff.use", CmdBuffItem, "buffitem"));
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }
            base.Dispose(disposing);
        }

        // /buffitem <damage>
        private void CmdBuffItem(CommandArgs args)
        {
            var p = args.Player;

            if (args.Parameters.Count < 1)
            {
                p.SendErrorMessage("ใช้แบบนี้: /buffitem <damage>");
                return;
            }

            if (!int.TryParse(args.Parameters[0], out int dmg) || dmg <= 0)
            {
                p.SendErrorMessage("damage ต้องเป็นตัวเลขมากกว่า 0");
                return;
            }

            var item = p.TPlayer.HeldItem;

            if (item == null || item.netID == 0)
            {
                p.SendErrorMessage("ไม่มีไอเทมในมือ");
                return;
            }

            int uid = GetOrMakeUID(item);

            _itemDamage[uid] = dmg;
            item.damage = dmg;

            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null,
                p.Index, p.TPlayer.selectedItem);

            p.SendSuccessMessage($"[Neko] บัฟไอเทม (UID={uid}) เป็น {dmg} damage แล้ว nya~");
        }

        private void OnGameUpdate(EventArgs args)
        {
            foreach (var ts in TShock.Players)
            {
                if (ts == null || !ts.Active)
                    continue;

                var item = ts.TPlayer.HeldItem;
                if (item == null || item.netID == 0)
                    continue;

                int uid = ExtractUID(item);
                if (uid == -1)
                    continue;

                if (!_itemDamage.TryGetValue(uid, out int dmg))
                    continue;

                if (item.damage != dmg)
                {
                    item.damage = dmg;

                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null,
                        ts.Index, ts.TPlayer.selectedItem);
                }
            }
        }

        // prefix 200–254 = UID
        private int GetOrMakeUID(Item item)
        {
            if (IsCustomPrefix(item.prefix))
                return item.prefix;

            int uid = Main.rand.Next(200, 255);
            item.prefix = (byte)uid;
            return uid;
        }

        private int ExtractUID(Item item)
        {
            return IsCustomPrefix(item.prefix) ? item.prefix : -1;
        }

        private bool IsCustomPrefix(int prefix)
        {
            return prefix is >= 200 and <= 254;
        }
    }
}
