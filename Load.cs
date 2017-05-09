using System;
using System.Linq;
using HesaEngine.SDK;
using HesaEngine.SDK.Enums;
using HesaEngine.SDK.GameObjects;
using SharpDX;

namespace SmiteEngine
{
    public class Load : IScript
    {
        public static bool IsSummonersRift => Game.MapId == GameMapId.SummonersRift;
        public static bool IsTwistedTreeline => Game.MapId == GameMapId.TwistedTreeline;
        public string Name => "SmiteEngine";
        public string Version => "1.1";
        public string Author => "Blackburn";
        public static Menu Menu;
        public static Spell SmiteSpell { get; set; }
        private static Obj_AI_Minion _minion;   
        private static readonly string[] SmiteMobs =
        {
            "SRU_Red", "SRU_Blue", "SRU_Dragon_Water", "SRU_Dragon_Fire", "SRU_Dragon_Earth", "SRU_Dragon_Air",
            "SRU_Dragon_Elder", "SRU_Baron", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_RiftHerald", "SRU_Krug",
            "Sru_Crab", "TT_Spiderboss", "TT_NGolem", "TT_NWolf", "TT_NWraith"
        };
       

        public void OnInitialize()
        {
            Game.OnGameLoaded += () =>
            {
                Core.DelayAction(Boot, 3000);
            };
        }


        public void Boot()
        {
            Chat.Print("SmiteEngine Version "+Version+" loaded!");
            CreateMyMenu();
            SmiteSlot();
            

        }


        public void SmiteSlot()
        {
            var smiteSlot =
                ObjectManager.Player.Spellbook.Spells.FirstOrDefault(x => x.SpellData.Name.ToLower().Contains("smite"));
            if (smiteSlot != null)
            {
                SmiteSpell = new Spell(smiteSlot.Slot, 570f, TargetSelector.DamageType.True);
                Game.OnUpdate += OnUpdate;
                Drawing.OnDraw += OnDraw;



            }
        }

        public void CreateMyMenu()
        {
         Menu = new Menu("SmiteEngine");
            var mainMenu = Menu.AddSubMenu("Main Menu");
            mainMenu.Add(new MenuCheckbox("SmiteEngine.Activated", "Activated", true));//.SetValue(new KeyBind(Key.N,MenuKeybindType.Toggle, false));
            mainMenu.Add(new MenuCheckbox("SmiteEngine.Range", "Smite Range", true));
            
            //Maps
            if (IsSummonersRift)
            {
                var small = Menu.AddSubMenu("Small Mobs");
                small.Add(new MenuCheckbox("SRU_Gromp", "Gromp").SetValue(false));
                small.Add(new MenuCheckbox("SRU_Razorbeak", "Raptors").SetValue(false));
                small.Add(new MenuCheckbox("Sru_Crab", "Crab").SetValue(false));
                small.Add(new MenuCheckbox("SRU_Krug", "Krug").SetValue(false));
                small.Add(new MenuCheckbox("SRU_Murkwolf", "Wolves").SetValue(false));

                var big = Menu.AddSubMenu("Big Mobs");
                big.Add(new MenuCheckbox("SRU_Baron", "Baron Nashor").SetValue(true));
                big.Add(new MenuCheckbox("SRU_RiftHerald", "Rift Herald").SetValue(true));
                big.Add(new MenuCheckbox("SRU_Dragon", "Dragons").SetValue(true));
                big.Add(new MenuCheckbox("SRU_Blue", "Blue Buff").SetValue(true));
                big.Add(new MenuCheckbox("SRU_Red", "Red Buff").SetValue(true));
            }

            if (IsTwistedTreeline)
            {
                var mobs = Menu.AddSubMenu("Mobs"); 
                mobs.Add(new MenuCheckbox("TT_Spiderboss", "Spiderman").SetValue(true));
                mobs.Add(new MenuCheckbox("TT_NGolem", "Golem Enabled").SetValue(true));
                mobs.Add(new MenuCheckbox("TT_NWolf", "Wolf Enabled").SetValue(true));
                mobs.Add(new MenuCheckbox("TT_NWraith", "Wraith Enabled").SetValue(true));
                
            }

            Menu.Add(new MenuCheckbox("SmiteEngine.KS", "Killsteal with Smite").SetValue(true));

        }


        private void OnDraw(EventArgs args)
        {
            if (Menu.Get<MenuCheckbox>("SmiteEngine.Range").Checked && Menu.Get<MenuCheckbox>("SmiteEngine.Activated").Checked)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, SmiteSpell.Range, Color.Violet);
            }
        }

        private static void SmiteKill()
        {
            if (!Menu.Get<MenuCheckbox>("SmiteEngine.KS").Checked)
            {
                return;
            }

            var smiteableEnemy = TargetSelector.GetTarget(570,TargetSelector.DamageType.True);
            if (smiteableEnemy == null)
            {
                return;
            }
            else if(20 + 8 * ObjectManager.Me.Level >= smiteableEnemy.Health)
            {
                ObjectManager.Player.Spellbook.CastSpell(SmiteSpell.Slot, smiteableEnemy);
                Logger.Log("Smited!");
            }


        }



        private static void OnUpdate()
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            if(!Menu.Get<MenuCheckbox>("SmiteEngine.Activated").Checked)
            {
                return;
            }
            SmiteKill();

            // MOBS EXCEPT DRAGON
            _minion = (Obj_AI_Minion) MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                570f, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(buff => buff.Name.StartsWith(buff.CharData.BaseSkinName)
                && SmiteMobs.Contains(buff.CharData.BaseSkinName)&& !buff.Name.Contains("Mini") && !buff.Name.Contains("Spawn"));

            if (_minion != null)
            {
                if (Menu.Get<MenuCheckbox>(_minion.CharData.BaseSkinName).Checked)
                {
                    if (SmiteSpell.IsReady())
                    {
                        if (Vector3.Distance(ObjectManager.Player.ServerPosition, _minion.ServerPosition) <=
                            570)
                        {
                            if (ObjectManager.Player.GetSummonerSpellDamage(_minion, Damage.SummonerSpell.Smite) >=
                                _minion.Health && SmiteSpell.CanCast(_minion))
                            {
                                ObjectManager.Player.Spellbook.CastSpell(SmiteSpell.Slot, _minion);
                                Logger.Log("Smited!");
                            }

                        }
                    }
                }
            }
            else if (Menu.Get<MenuCheckbox>("SRU_Dragon").Checked)
            {
                if (SmiteSpell.IsReady())
                {

                    if (ObjectManager.Dragon.IsInRange(ObjectManager.Player, 570))
                    {
                        if (ObjectManager.Player.GetSummonerSpellDamage(ObjectManager.Dragon,
                                Damage.SummonerSpell.Smite) >= ObjectManager.Dragon.Health &&
                            SmiteSpell.CanCast(ObjectManager.Dragon))
                        {
                            ObjectManager.Player.Spellbook.CastSpell(SmiteSpell.Slot, ObjectManager.Dragon);
                        }
                    }
                }
            }
            else
            {
                SmiteKill();
            }
        }
    }
    }

