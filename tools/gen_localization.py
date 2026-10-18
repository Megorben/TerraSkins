"""Writes Localization/en-US_Mods.TerraSkins.hjson and ru-RU_Mods.TerraSkins.hjson.

Item lists are read from the C# sources (effect catalog and set list) so every item is covered.
Run: python3 tools/gen_localization.py
"""
import json
import os
import re

ROOT = os.path.join(os.path.dirname(__file__), "..")

# name: (english, russian)
EFFECT_NAMES = {
    "EventHorizon": ("Event Horizon", "Горизонт событий"),
    "VioletSingularity": ("Violet Singularity", "Фиолетовая сингулярность"),
    "AzureAbyss": ("Azure Abyss", "Лазурная бездна"),
    "Quasar": ("Quasar", "Квазар"),
    "MilkyWay": ("Milky Way", "Млечный Путь"),
    "NebulaGalaxy": ("Nebula Galaxy", "Галактика туманностей"),
    "EmeraldGalaxy": ("Emerald Galaxy", "Изумрудная галактика"),
    "SpiritSoulfire": ("Spirit Soulfire", "Духовное пламя"),
    "CursedSoulfire": ("Cursed Soulfire", "Проклятое пламя"),
    "ShadowSoulfire": ("Shadow Soulfire", "Теневое пламя"),
    "SolarSoulfire": ("Solar Soulfire", "Солнечное пламя"),
    "DivineRadiance": ("Divine Radiance", "Божественное сияние"),
    "MoonlitRadiance": ("Moonlit Radiance", "Лунное сияние"),
    "PrismaticRadiance": ("Prismatic Radiance", "Призматическое сияние"),
    "GreatBear": ("Great Bear", "Большая Медведица"),
    "Hunter": ("The Hunter", "Охотник"),
    "VoidMaw": ("Void Maw", "Пасть Пустоты"),
    "SakuraStorm": ("Sakura Storm", "Буря сакуры"),
    "SpringBreeze": ("Spring Breeze", "Весенний ветер"),
    "AutumnGale": ("Autumn Gale", "Осенний листопад"),
    "Blizzard": ("Blizzard", "Метель"),
    "Thunderstorm": ("Thunderstorm", "Гроза"),
    "BloodStorm": ("Blood Storm", "Кровавая гроза"),
    "DeepSea": ("Deep Sea", "Морская глубина"),
    "Lovestruck": ("Lovestruck", "Влюблённость"),
    "Melody": ("Melody", "Мелодия"),
    "NebulaVeil": ("Nebula Veil", "Вуаль туманности"),
    "AuroraVeil": ("Aurora Veil", "Вуаль сияния"),
    "AngelHalo": ("Angel Halo", "Ангельский нимб"),
    "MoonHalo": ("Moon Halo", "Лунный нимб"),
    "RoseHalo": ("Rose Halo", "Розовый нимб"),
    "PrismHalo": ("Prism Halo", "Радужный нимб"),
    "FallenHalo": ("Fallen Halo", "Нимб падшего"),
    "VoidHalo": ("Void Halo", "Нимб Пустоты"),
    "SingularityCrown": ("Singularity Crown", "Корона сингулярности"),
    "VoidCrown": ("Void Crown", "Корона Пустоты"),
    "StarCrown": ("Star Crown", "Звёздная корона"),
    "ArcaneHalo": ("Arcane Halo", "Тайный нимб"),
    "InfernalHalo": ("Infernal Halo", "Адский нимб"),
    "RingedPlanet": ("Ringed Planet", "Планета с кольцами"),
    "SpiritFlameCrown": ("Spirit Flame Crown", "Корона духовного огня"),
    "SolarFlameCrown": ("Solar Flame Crown", "Корона солнечного огня"),
    "GoldenStars": ("Golden Stars", "Золотые звёзды"),
    "AzureStars": ("Azure Stars", "Лазурные звёзды"),
    "PrismStars": ("Prism Stars", "Радужные звёзды"),
    "AmethystShards": ("Amethyst Shards", "Аметистовые осколки"),
    "EmeraldShards": ("Emerald Shards", "Изумрудные осколки"),
    "RubyShards": ("Ruby Shards", "Рубиновые осколки"),
    "SapphireShards": ("Sapphire Shards", "Сапфировые осколки"),
    "TinyMoons": ("Tiny Moons", "Маленькие луны"),
    "SpiritWisps": ("Spirit Wisps", "Блуждающие духи"),
    "CursedWisps": ("Cursed Wisps", "Проклятые огоньки"),
    "SpectralBlades": ("Spectral Blades", "Призрачные клинки"),
    "ShadowBlades": ("Shadow Blades", "Теневые клинки"),
    "Atom": ("Atom", "Атом"),
    "Butterflies": ("Butterflies", "Бабочки"),
    "MorphoButterflies": ("Morpho Butterflies", "Бабочки-морфо"),
    "Stardust": ("Stardust", "Звёздная пыль"),
    "AzureStardust": ("Azure Stardust", "Лазурная пыль"),
    "RoseStardust": ("Rose Stardust", "Розовая пыль"),
    "RainbowRibbon": ("Rainbow Ribbon", "Радужная лента"),
    "NebulaRibbon": ("Nebula Ribbon", "Лента туманности"),
    "SolarFlameTrail": ("Solar Flame Trail", "Солнечный огненный след"),
    "CursedFlameTrail": ("Cursed Flame Trail", "Проклятый огненный след"),
    "SpiritFlameTrail": ("Spirit Flame Trail", "Духовный огненный след"),
    "VoidTrail": ("Void Trail", "След Пустоты"),
    "SakuraTrail": ("Sakura Trail", "След сакуры"),
    "AutumnTrail": ("Autumn Trail", "Осенний след"),
    "LightningTrail": ("Lightning Trail", "Грозовой след"),
    "FrostTrail": ("Frost Trail", "Морозный след"),
    "BubbleTrail": ("Bubble Trail", "След пузырей"),
    "PhantomAfterimage": ("Phantom Afterimage", "Фантомный шлейф"),
    "ShadowAfterimage": ("Shadow Afterimage", "Теневой шлейф"),
    "PrismAfterimage": ("Prism Afterimage", "Радужный шлейф"),
    "SeraphWings": ("Seraph Wings", "Крылья серафима"),
    "AzureSeraphWings": ("Azure Seraph Wings", "Лазурные крылья серафима"),
    "RoseSeraphWings": ("Rose Seraph Wings", "Розовые крылья серафима"),
    "VoidTendrils": ("Void Tendrils", "Щупальца Пустоты"),
    "AbyssTendrils": ("Abyss Tendrils", "Щупальца бездны"),
    "PrismaticWings": ("Prismatic Wings", "Призматические крылья"),
    "AmethystWings": ("Amethyst Wings", "Аметистовые крылья"),
    "PhoenixWings": ("Phoenix Wings", "Крылья феникса"),
    "SpiritFlameWings": ("Spirit Flame Wings", "Крылья духовного огня"),
    "ConstellationWings": ("Constellation Wings", "Крылья созвездий"),
    "ArcaneCircle": ("Arcane Circle", "Тайный круг"),
    "InfernalCircle": ("Infernal Circle", "Адский круг"),
    "HolyCircle": ("Holy Circle", "Святой круг"),
    "FlowerSteps": ("Flower Steps", "Цветочные шаги"),
    "SakuraSteps": ("Sakura Steps", "Шаги сакуры"),
    "FrostSteps": ("Frost Steps", "Ледяные шаги"),
    "ShadowPool": ("Shadow Pool", "Теневая лужа"),
    "Sunbeam": ("Sunbeam", "Солнечный луч"),
}

CLASS_TOOLTIPS = {
    "BlackHoleAura": ("A black hole devours the light behind you", "За вашей спиной чёрная дыра пожирает свет"),
    "GalaxyAura": ("A whole galaxy turns slowly behind you", "За вашей спиной медленно вращается целая галактика"),
    "SoulFlameAura": ("Ghostly flames lick up your body", "Призрачное пламя окутывает тело"),
    "DivineRaysAura": ("Radiant rays shine from behind you", "Из-за спины сияют лучи света"),
    "ConstellationAura": ("A constellation lights up around you", "Вокруг вас загорается созвездие"),
    "VoidAura": ("Something watches from the dark smoke...", "Из тёмного дыма что-то наблюдает..."),
    "PetalStormAura": ("Petals and leaves swirl around you", "Вокруг вас кружатся лепестки и листья"),
    "BlizzardAura": ("Snow swirls around you", "Вокруг вас кружится снег"),
    "StormAura": ("Lightning crackles around you", "Вокруг вас трещат молнии"),
    "BubbleAura": ("Bubbles drift up from you", "От вас поднимаются пузыри"),
    "HeartsAura": ("Hearts float up around you", "Вокруг вас взлетают сердечки"),
    "MelodyAura": ("Music notes dance around you", "Вокруг вас танцуют ноты"),
    "NebulaAura": ("Soft nebula clouds churn behind you", "За спиной клубятся облака туманности"),
    "AngelHalo": ("A shining halo floats above your head", "Над головой парит сияющий нимб"),
    "FallenHalo": ("A dark halo wreathed in flames", "Тёмный нимб в языках пламени"),
    "SingularityHalo": ("A tiny black hole spins above your head", "Над головой вращается маленькая чёрная дыра"),
    "StarCrownHalo": ("A circlet of stars turns above your head", "Над головой кружится венец из звёзд"),
    "RuneHalo": ("Glowing runes circle above your head", "Над головой кружат светящиеся руны"),
    "PlanetHalo": ("A ringed planet with its moons hovers above you", "Над вами парит планета с кольцами и лунами"),
    "FlameCrownHalo": ("A crown of flickering flames", "Корона из мерцающего пламени"),
    "StarOrbit": ("Stars orbit around you", "Вокруг вас вращаются звёзды"),
    "CrystalOrbit": ("Crystal shards orbit around you", "Вокруг вас вращаются осколки кристаллов"),
    "MoonOrbit": ("Two little moons orbit around you", "Вокруг вас вращаются две маленькие луны"),
    "WispOrbit": ("Wisps dance around you", "Вокруг вас танцуют огоньки"),
    "BladeOrbit": ("A ring of spectral blades circles you", "Вокруг вас кружит кольцо клинков"),
    "AtomOrbit": ("Electrons orbit around you", "Вокруг вас вращаются электроны"),
    "ButterflyOrbit": ("Butterflies flutter around you", "Вокруг вас порхают бабочки"),
    "StardustTrail": ("Leaves twinkling stardust as you move", "При движении оставляет мерцающую звёздную пыль"),
    "RibbonTrail": ("A ribbon of light follows you", "За вами тянется лента света"),
    "FlameTrail": ("Leaves a trail of flames as you move", "При движении оставляет огненный след"),
    "VoidTrail": ("Leaves dark smoke as you move", "При движении оставляет тёмный дым"),
    "PetalTrail": ("Petals fall behind you as you move", "При движении за вами осыпаются лепестки"),
    "LightningTrail": ("Lightning arcs where you passed", "Там, где вы прошли, бьют молнии"),
    "FrostTrail": ("Snowflakes and ice fall behind you", "За вами падают снежинки и лёд"),
    "BubbleTrail": ("Leaves bubbles as you move", "При движении оставляет пузыри"),
    "AfterimageTrail": ("Glowing copies of you follow your movement", "За вами следуют светящиеся копии"),
    "SeraphWings": ("Great wings of light", "Огромные крылья из света"),
    "VoidTendrilWings": ("Writhing tendrils of the void", "Извивающиеся щупальца Пустоты"),
    "CrystalWings": ("Shimmering crystal wings", "Мерцающие кристальные крылья"),
    "PhoenixWings": ("Wings of living fire", "Крылья живого огня"),
    "ConstellationWings": ("Wings traced by stars", "Крылья, начерченные звёздами"),
    "MagicCircleGround": ("A magic circle glows under your feet", "Под ногами светится магический круг"),
    "FootstepGround": ("Things grow where you step", "Там, где вы ступаете, что-то вырастает"),
    "ShadowPoolGround": ("A pool of shadow follows your feet", "Под ногами растекается тень"),
    "SunbeamGround": ("A pillar of soft light rises from the ground", "Из земли поднимается столб мягкого света"),
}

SET_NAMES = {
    "VoidSovereign": ("Void Sovereign", "Повелитель Пустоты", "Повелителя Пустоты"),
    "CelestialSeraph": ("Celestial Seraph", "Небесный серафим", "небесного серафима"),
    "AstralVoyager": ("Astral Voyager", "Звёздный странник", "звёздного странника"),
    "InfernalWarlord": ("Infernal Warlord", "Адский полководец", "адского полководца"),
    "FrostMonarch": ("Frost Monarch", "Ледяной монарх", "ледяного монарха"),
    "SakuraRonin": ("Sakura Ronin", "Ронин сакуры", "ронина сакуры"),
    "NeonPhantom": ("Neon Phantom", "Неоновый фантом", "неонового фантома"),
    "VerdantDruid": ("Verdant Druid", "Зелёный друид", "зелёного друида"),
    "AbyssalTide": ("Abyssal Tide", "Прилив бездны", "прилива бездны"),
    "PrismaticParagon": ("Prismatic Paragon", "Призматический идеал", "призматического идеала"),
}

HEAD_EN = {"FullHelmet": "Helm", "Hat": "Hat", "Circlet": "Circlet"}
HEAD_RU = {"FullHelmet": "Шлем", "Hat": "Шляпа", "Circlet": "Венец"}
HOOD = {"AstralVoyager": ("Hood", "Капюшон")}

SLOTS = {
    "Head": ("Head", "Голова"), "Body": ("Body", "Тело"), "Legs": ("Legs", "Ноги"), "Wings": ("Wings", "Крылья"),
    "Back": ("Back / cape", "Спина / плащ"), "Face": ("Face", "Лицо"), "Neck": ("Neck", "Шея"), "Hands": ("Hands", "Руки"),
    "Waist": ("Waist", "Пояс"), "Shoes": ("Shoes", "Обувь"), "Shield": ("Shield", "Щит"), "Balloon": ("Balloon", "Шарик"),
    "Aura": ("Aura", "Аура"), "Halo": ("Halo", "Нимб"), "Orbit": ("Orbit", "Орбита"), "Trail": ("Trail", "След"),
    "Ground": ("Ground", "Под ногами"),
}

UI = {
    "Title": ("TerraSkins Wardrobe", "Гардероб TerraSkins"),
    "Close": ("Close", "Закрыть"),
    "OpenWardrobe": ("Open the TerraSkins wardrobe", "Открыть гардероб TerraSkins"),
    "WardrobeOn": ("Wardrobe: ON", "Гардероб: ВКЛ"),
    "WardrobeOff": ("Wardrobe: OFF", "Гардероб: ВЫКЛ"),
    "WardrobeToggleTooltip": ("Quickly show your real equipment without losing your look", "Быстро показать настоящую экипировку, не теряя образ"),
    "Randomize": ("Surprise me!", "Случайный образ"),
    "RandomizeTooltip": ("Puts together a random look", "Собирает случайный образ"),
    "ClearAll": ("Clear everything", "Очистить всё"),
    "ClearAllTooltip": ("Removes every override, your own equipment shows again", "Снимает все замены, снова видна ваша экипировка"),
    "Outfits": ("Outfits", "Образы"),
    "SearchHint": ("Search...", "Поиск..."),
    "AllSources": ("All", "Все"),
    "Dye": ("Dye", "Краситель"),
    "BackToLooks": ("Back", "Назад"),
    "DyeTooltip": ("Pick a dye for the appearance in this slot", "Выбрать краситель для облика в этом слоте"),
    "DyeModeHint": ("Choosing a dye: {0}", "Выбор красителя: {0}"),
    "ResetSlot": ("Reset slot", "Сбросить слот"),
    "ResetSlotTooltip": ("Show your own equipment in this slot", "Показывать в этом слоте вашу экипировку"),
    "KeepOwn": ("your own equipment", "своя экипировка"),
    "KeepOwnTooltip": ("Your own equipment\nThe wardrobe does not change this slot", "Своя экипировка\nГардероб не меняет этот слот"),
    "Hidden": ("hidden", "скрыто"),
    "HideTooltip": ("Hidden\nNothing is shown in this slot, even if you wear something", "Скрыто\nВ этом слоте ничего не видно, даже если вы что-то носите"),
    "NoDyeTooltip": ("No dye", "Без красителя"),
    "PreviewHint": ("Left click: walk / stand\nRight click: turn around", "Левый клик: идти / стоять\nПравый клик: развернуть"),
    "SaveOutfit": ("Save current look", "Сохранить текущий образ"),
    "SaveOutfitHint": ("type a name in the search box first", "сначала можно ввести имя в поиске"),
    "SaveOutfitTooltip": ("Saves your current look as an outfit\nTip: the text in the search box becomes its name", "Сохраняет текущий образ\nСовет: текст из поля поиска станет его названием"),
    "PresetOutfit": ("TerraSkins outfit", "Образ TerraSkins"),
    "SavedOutfit": ("your outfit", "ваш образ"),
    "PresetTooltip": ("{0}\nLeft click: wear the whole outfit", "{0}\nЛевый клик: надеть весь образ"),
    "SavedTooltip": ("{0}\nLeft click: wear\nRight click: delete", "{0}\nЛевый клик: надеть\nПравый клик: удалить"),
    "DefaultOutfitName": ("Look {0}", "Образ {0}"),
}

TOOLTIPS = {
    "Slot": ("Wardrobe slot: {0}", "Слот гардероба: {0}"),
    "Cosmetic": ("Purely cosmetic: no stats, no light, no advantage", "Только для красоты: без характеристик, света и преимуществ"),
}

CONFIG = {
    "DisplayName": ("TerraSkins", "TerraSkins"),
    "Headers.Effects": ("Effects", "Эффекты"),
    "Headers.Interface": ("Interface", "Интерфейс"),
    "ShowOwnEffects.Label": ("Show my effects", "Показывать мои эффекты"),
    "ShowOwnEffects.Tooltip": ("Draw your own auras, halos, trails and other effects", "Рисовать ваши ауры, нимбы, следы и другие эффекты"),
    "ShowOtherPlayersEffects.Label": ("Show other players' effects", "Показывать эффекты других игроков"),
    "ShowOtherPlayersEffects.Tooltip": ("Turn off if effects of other players distract you", "Выключите, если эффекты других игроков отвлекают"),
    "EffectOpacity.Label": ("Effect opacity", "Непрозрачность эффектов"),
    "EffectOpacity.Tooltip": ("How visible cosmetic effects are", "Насколько заметны косметические эффекты"),
    "ShowWardrobeButton.Label": ("Show the wardrobe button", "Показывать кнопку гардероба"),
    "ShowWardrobeButton.Tooltip": ("The hanger button among the builder toggles in the inventory", "Кнопка-вешалка среди переключателей строителя в инвентаре"),
    "ShowPreviewEffects.Label": ("Effects in the wardrobe preview", "Эффекты в предпросмотре гардероба"),
    "ShowPreviewEffects.Tooltip": ("Draw effects on the character preview of the wardrobe", "Рисовать эффекты на персонаже в окне гардероба"),
}


def read_effects():
    src = open(os.path.join(ROOT, "Common", "Effects", "Library", "SkinEffectCatalog.cs")).read()
    return re.findall(r'new (\w+)\("(\w+)", Palettes\.\w+', src)


def read_sets():
    src = open(os.path.join(ROOT, "Content", "Items", "Sets", "SkinSet.cs")).read()
    return re.findall(r'new\("(\w+)", HeadStyle\.(\w+), (true|false),', src)


def q(s):
    return json.dumps(s, ensure_ascii=False)


def build(lang):
    L = 0 if lang == "en-US" else 1
    out = ["Mods: {", "\tTerraSkins: {"]

    def section(name, entries, depth=2):
        tabs = "\t" * depth
        out.append(f"{tabs}{name}: {{")
        for key, value in entries:
            if isinstance(value, list):
                section(key, value, depth + 1)
            else:
                out.append(f"{tabs}\t{key}: {q(value)}")
        out.append(f"{tabs}}}")

    items = []
    for cls, name in read_effects():
        items.append((name, [("DisplayName", EFFECT_NAMES[name][L]), ("Tooltip", CLASS_TOOLTIPS[cls][L])]))
    for name, head, robe in read_sets():
        set_en, set_ru, set_ru_gen = SET_NAMES[name]
        head_en, head_ru = HOOD.get(name, (HEAD_EN[head], HEAD_RU[head]))
        body_en, body_ru = ("Robe", "Мантия") if robe == "true" else ("Armor", "Доспех")
        tip = ("Part of the {0} set" if L == 0 else "Часть комплекта «{0}»").format(set_en if L == 0 else set_ru)
        items.append((f"{name}Head", [("DisplayName", f"{set_en} {head_en}" if L == 0 else f"{head_ru} {set_ru_gen}"), ("Tooltip", tip)]))
        items.append((f"{name}Body", [("DisplayName", f"{set_en} {body_en}" if L == 0 else f"{body_ru} {set_ru_gen}"), ("Tooltip", tip)]))
        if robe != "true":
            items.append((f"{name}Legs", [("DisplayName", f"{set_en} Greaves" if L == 0 else f"Поножи {set_ru_gen}"), ("Tooltip", tip)]))
    section("Items", items)

    section("Sets", [(k, v[L]) for k, v in SET_NAMES.items()])
    section("Slots", [(k, v[L]) for k, v in SLOTS.items()])
    section("Tooltips", [(k, v[L]) for k, v in TOOLTIPS.items()])
    section("UI", [(k, v[L]) for k, v in UI.items()])
    section("Keybinds", [("OpenWardrobe", [("DisplayName", "Open wardrobe" if L == 0 else "Открыть гардероб")])])
    section("BuilderToggles", [("WardrobeBuilderToggle", [("DisplayName", UI["OpenWardrobe"][L])])])

    cfg = {}
    for key, value in CONFIG.items():
        parts = key.split(".")
        node = cfg
        for p in parts[:-1]:
            node = node.setdefault(p, {})
        node[parts[-1]] = value[L]

    def to_entries(d):
        return [(k, to_entries(v) if isinstance(v, dict) else v) for k, v in d.items()]
    section("Configs", [("TerraSkinsClientConfig", to_entries(cfg))])

    out += ["\t}", "}", ""]
    path = os.path.join(ROOT, "Localization", f"{lang}_Mods.TerraSkins.hjson")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "w", encoding="utf-8").write("\n".join(out))


if __name__ == "__main__":
    build("en-US")
    build("ru-RU")
    print("wrote localization")
