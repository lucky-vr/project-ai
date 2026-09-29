using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VillageDraft.Editor
{
    /// <summary>AI-assisted reference model paths and placement hints; no scene mutations.</summary>
    public static class VillageModelCatalog
    {
        public sealed class VenueAssets
        {
            public readonly string Id;
            public readonly string ExteriorPath;
            public readonly string[] PropPaths;
            public readonly string[] PropRoles;
            public readonly Vector3[] SuggestedLocalPositions;
            public readonly float[] SuggestedYaws;
            public readonly float[] SuggestedScales;
            public readonly string[] PreferredActionObjects;
            public readonly float[] ActionVisualScales;
            public VenueAssets(string id, string exteriorPath, string[] propPaths,
                string[] propRoles, Vector3[] positions, float[] yaws, float[] scales,
                string[] preferredActions, float[] actionScales)
            {
                Id=id; ExteriorPath=exteriorPath; PropPaths=propPaths; PropRoles=propRoles;
                SuggestedLocalPositions=positions; SuggestedYaws=yaws; SuggestedScales=scales;
                PreferredActionObjects=preferredActions; ActionVisualScales=actionScales;
            }
        }

        public static readonly string[] VenueIds = {
            "market", "cafe", "workshop", "greenhouse", "pool", "bakery", "post", "clinic", "library", "music", "garage", "observatory", "harbor", "art", "recycling"
        };
        private static readonly Dictionary<string, VenueAssets> Entries =
            new Dictionary<string, VenueAssets>(StringComparer.OrdinalIgnoreCase)
        {
            { "market", new VenueAssets(
                "market", "Assets/VillageDraft/Models/FBX/market/market_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/market/market_produce_stand.fbx", "Assets/VillageDraft/Models/FBX/market/market_wicker_basket.fbx", "Assets/VillageDraft/Models/FBX/market/market_checkout_register.fbx", "Assets/VillageDraft/Models/FBX/market/market_wooden_cart.fbx", "Assets/VillageDraft/Models/FBX/market/market_fruit_scale.fbx", "Assets/VillageDraft/Models/FBX/market/market_bread_loaf.fbx", "Assets/VillageDraft/Models/FBX/market/market_red_apple.fbx" },
                new[] { "display", "basket", "machine", "cart", "scale", "bread", "apple" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "Basket", "Register", "", "", "Grab bread", "Grab apple" },
                new[] { 1.0000f, 0.9200f, 0.7800f, 1.0000f, 1.0000f, 0.4200f, 0.5500f }) },
            { "cafe", new VenueAssets(
                "cafe", "Assets/VillageDraft/Models/FBX/cafe/cafe_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/cafe/cafe_espresso_machine.fbx", "Assets/VillageDraft/Models/FBX/cafe/cafe_cafe_counter.fbx", "Assets/VillageDraft/Models/FBX/cafe/cafe_cafe_table.fbx", "Assets/VillageDraft/Models/FBX/cafe/cafe_bar_stool.fbx", "Assets/VillageDraft/Models/FBX/cafe/cafe_coffee_grinder.fbx", "Assets/VillageDraft/Models/FBX/cafe/cafe_serving_tray.fbx", "Assets/VillageDraft/Models/FBX/cafe/cafe_serving_cup.fbx" },
                new[] { "machine", "counter", "table", "stool", "grinder", "tray", "cup" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "Coffee machine", "", "", "", "", "", "Grab coffee cup" },
                new[] { 0.8200f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 0.5200f }) },
            { "workshop", new VenueAssets(
                "workshop", "Assets/VillageDraft/Models/FBX/workshop/workshop_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/workshop/workshop_windmill_tower.fbx", "Assets/VillageDraft/Models/FBX/workshop/workshop_workbench.fbx", "Assets/VillageDraft/Models/FBX/workshop/workshop_tool_rack.fbx", "Assets/VillageDraft/Models/FBX/workshop/workshop_power_console.fbx", "Assets/VillageDraft/Models/FBX/workshop/workshop_gear_assembly.fbx", "Assets/VillageDraft/Models/FBX/workshop/workshop_toolbox.fbx", "Assets/VillageDraft/Models/FBX/workshop/workshop_repair_vise.fbx" },
                new[] { "windmill", "counter", "rack", "machine", "gear", "crate", "vise" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "greenhouse", new VenueAssets(
                "greenhouse", "Assets/VillageDraft/Models/FBX/greenhouse/greenhouse_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/greenhouse/greenhouse_planting_bench.fbx", "Assets/VillageDraft/Models/FBX/greenhouse/greenhouse_water_pump.fbx", "Assets/VillageDraft/Models/FBX/greenhouse/greenhouse_seed_cabinet.fbx", "Assets/VillageDraft/Models/FBX/greenhouse/greenhouse_clay_planter.fbx", "Assets/VillageDraft/Models/FBX/greenhouse/greenhouse_garden_trellis.fbx", "Assets/VillageDraft/Models/FBX/greenhouse/greenhouse_watering_can.fbx", "Assets/VillageDraft/Models/FBX/greenhouse/greenhouse_compost_barrel.fbx" },
                new[] { "counter", "pump", "cabinet", "plant", "trellis", "can", "barrel" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "pool", new VenueAssets(
                "pool", "Assets/VillageDraft/Models/FBX/pool/pool_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/pool/pool_lifeguard_chair.fbx", "Assets/VillageDraft/Models/FBX/pool/pool_ring_target.fbx", "Assets/VillageDraft/Models/FBX/pool/pool_diving_board.fbx", "Assets/VillageDraft/Models/FBX/pool/pool_pool_lounger.fbx", "Assets/VillageDraft/Models/FBX/pool/pool_changing_screen.fbx", "Assets/VillageDraft/Models/FBX/pool/pool_toss_ring.fbx", "Assets/VillageDraft/Models/FBX/pool/pool_towel_cart.fbx" },
                new[] { "chair", "ring", "board", "lounger", "screen", "tossring", "cart" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "Grab and toss ring", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "bakery", new VenueAssets(
                "bakery", "Assets/VillageDraft/Models/FBX/bakery/bakery_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/bakery/bakery_bread_oven.fbx", "Assets/VillageDraft/Models/FBX/bakery/bakery_dough_counter.fbx", "Assets/VillageDraft/Models/FBX/bakery/bakery_rolling_rack.fbx", "Assets/VillageDraft/Models/FBX/bakery/bakery_dough_mixer.fbx", "Assets/VillageDraft/Models/FBX/bakery/bakery_baguette_basket.fbx", "Assets/VillageDraft/Models/FBX/bakery/bakery_pastry_display.fbx", "Assets/VillageDraft/Models/FBX/bakery/bakery_flour_barrel.fbx" },
                new[] { "oven", "counter", "rack", "mixer", "basket", "display", "barrel" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "post", new VenueAssets(
                "post", "Assets/VillageDraft/Models/FBX/post/post_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/post/post_sorting_counter.fbx", "Assets/VillageDraft/Models/FBX/post/post_mail_slots.fbx", "Assets/VillageDraft/Models/FBX/post/post_parcel_trolley.fbx", "Assets/VillageDraft/Models/FBX/post/post_stamp_press.fbx", "Assets/VillageDraft/Models/FBX/post/post_letter_scale.fbx", "Assets/VillageDraft/Models/FBX/post/post_postal_box.fbx", "Assets/VillageDraft/Models/FBX/post/post_wrapping_station.fbx" },
                new[] { "counter", "cabinet", "cart", "press", "scale", "mailbox", "table" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "clinic", new VenueAssets(
                "clinic", "Assets/VillageDraft/Models/FBX/clinic/clinic_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/clinic/clinic_examination_bed.fbx", "Assets/VillageDraft/Models/FBX/clinic/clinic_medicine_cabinet.fbx", "Assets/VillageDraft/Models/FBX/clinic/clinic_heart_monitor.fbx", "Assets/VillageDraft/Models/FBX/clinic/clinic_first_aid_cart.fbx", "Assets/VillageDraft/Models/FBX/clinic/clinic_treatment_lamp.fbx", "Assets/VillageDraft/Models/FBX/clinic/clinic_clinic_desk.fbx", "Assets/VillageDraft/Models/FBX/clinic/clinic_medical_tray.fbx" },
                new[] { "bed", "cabinet", "monitor", "cart", "lamp", "table", "tray" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "library", new VenueAssets(
                "library", "Assets/VillageDraft/Models/FBX/library/library_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/library/library_bookcase.fbx", "Assets/VillageDraft/Models/FBX/library/library_reading_desk.fbx", "Assets/VillageDraft/Models/FBX/library/library_book_return_cart.fbx", "Assets/VillageDraft/Models/FBX/library/library_story_podium.fbx", "Assets/VillageDraft/Models/FBX/library/library_reading_lamp.fbx", "Assets/VillageDraft/Models/FBX/library/library_open_book.fbx", "Assets/VillageDraft/Models/FBX/library/library_study_bench.fbx" },
                new[] { "cabinet", "table", "cart", "podium", "lamp", "book", "bench" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "music", new VenueAssets(
                "music", "Assets/VillageDraft/Models/FBX/music/music_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/music/music_stage_piano.fbx", "Assets/VillageDraft/Models/FBX/music/music_concert_drum.fbx", "Assets/VillageDraft/Models/FBX/music/music_microphone_stand.fbx", "Assets/VillageDraft/Models/FBX/music/music_music_stand.fbx", "Assets/VillageDraft/Models/FBX/music/music_speaker_stack.fbx", "Assets/VillageDraft/Models/FBX/music/music_stage_spotlight.fbx", "Assets/VillageDraft/Models/FBX/music/music_instrument_case.fbx" },
                new[] { "piano", "drum", "microphone", "stand", "speaker", "lamp", "case" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "garage", new VenueAssets(
                "garage", "Assets/VillageDraft/Models/FBX/garage/garage_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/garage/garage_vehicle_lift.fbx", "Assets/VillageDraft/Models/FBX/garage/garage_mechanic_bench.fbx", "Assets/VillageDraft/Models/FBX/garage/garage_tire_rack.fbx", "Assets/VillageDraft/Models/FBX/garage/garage_engine_block.fbx", "Assets/VillageDraft/Models/FBX/garage/garage_fuel_pump.fbx", "Assets/VillageDraft/Models/FBX/garage/garage_service_trolley.fbx", "Assets/VillageDraft/Models/FBX/garage/garage_traffic_cone.fbx" },
                new[] { "lift", "counter", "rack", "engine", "pump", "cart", "cone" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "observatory", new VenueAssets(
                "observatory", "Assets/VillageDraft/Models/FBX/observatory/observatory_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/observatory/observatory_astronomy_telescope.fbx", "Assets/VillageDraft/Models/FBX/observatory/observatory_star_chart_desk.fbx", "Assets/VillageDraft/Models/FBX/observatory/observatory_orrery.fbx", "Assets/VillageDraft/Models/FBX/observatory/observatory_observation_podium.fbx", "Assets/VillageDraft/Models/FBX/observatory/observatory_lens_cabinet.fbx", "Assets/VillageDraft/Models/FBX/observatory/observatory_moon_globe.fbx", "Assets/VillageDraft/Models/FBX/observatory/observatory_tripod_beacon.fbx" },
                new[] { "telescope", "table", "orrery", "podium", "cabinet", "globe", "lamp" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "harbor", new VenueAssets(
                "harbor", "Assets/VillageDraft/Models/FBX/harbor/harbor_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/harbor/harbor_fishing_boat.fbx", "Assets/VillageDraft/Models/FBX/harbor/harbor_mooring_bollard.fbx", "Assets/VillageDraft/Models/FBX/harbor/harbor_cargo_crane.fbx", "Assets/VillageDraft/Models/FBX/harbor/harbor_fish_crate.fbx", "Assets/VillageDraft/Models/FBX/harbor/harbor_life_buoy.fbx", "Assets/VillageDraft/Models/FBX/harbor/harbor_harbor_barrel.fbx", "Assets/VillageDraft/Models/FBX/harbor/harbor_dock_lantern.fbx" },
                new[] { "boat", "bollard", "crane", "crate", "ring", "barrel", "lamp" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "art", new VenueAssets(
                "art", "Assets/VillageDraft/Models/FBX/art/art_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/art/art_painting_easel.fbx", "Assets/VillageDraft/Models/FBX/art/art_sculpture_plinth.fbx", "Assets/VillageDraft/Models/FBX/art/art_paint_trolley.fbx", "Assets/VillageDraft/Models/FBX/art/art_pottery_wheel.fbx", "Assets/VillageDraft/Models/FBX/art/art_art_canvas.fbx", "Assets/VillageDraft/Models/FBX/art/art_brush_rack.fbx", "Assets/VillageDraft/Models/FBX/art/art_palette_table.fbx" },
                new[] { "easel", "podium", "cart", "wheel", "canvas", "rack", "table" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
            { "recycling", new VenueAssets(
                "recycling", "Assets/VillageDraft/Models/FBX/recycling/recycling_exterior.fbx",
                new[] { "Assets/VillageDraft/Models/FBX/recycling/recycling_sorting_bins.fbx", "Assets/VillageDraft/Models/FBX/recycling/recycling_bottle_crusher.fbx", "Assets/VillageDraft/Models/FBX/recycling/recycling_conveyor.fbx", "Assets/VillageDraft/Models/FBX/recycling/recycling_collection_cart.fbx", "Assets/VillageDraft/Models/FBX/recycling/recycling_glass_crate.fbx", "Assets/VillageDraft/Models/FBX/recycling/recycling_compost_tumbler.fbx", "Assets/VillageDraft/Models/FBX/recycling/recycling_recycle_scale.fbx" },
                new[] { "bins", "press", "conveyor", "cart", "crate", "tumbler", "scale" },
                new[] { new Vector3(-4.3500f, 0.1000f, 2.7500f), new Vector3(-4.3500f, 0.1000f, 0.1000f), new Vector3(-4.3500f, 0.1000f, -2.5500f), new Vector3(4.3500f, 0.1000f, 2.7500f), new Vector3(4.3500f, 0.1000f, 0.1000f), new Vector3(4.3500f, 0.1000f, -2.5500f), new Vector3(0.0000f, 0.1000f, 3.4500f) },
                new[] { -90.0000f, -90.0000f, -90.0000f, 90.0000f, 90.0000f, 90.0000f, 0.0000f },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f },
                new[] { "", "", "", "", "", "", "" },
                new[] { 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f }) },
        };

        public static IEnumerable<VenueAssets> All => Entries.Values;
        public static VenueAssets Get(string venueId)
        {
            if (!Entries.TryGetValue(venueId, out var entry))
                throw new ArgumentException("Unknown village venue: " + venueId, nameof(venueId));
            return entry;
        }

        public static GameObject LoadExterior(string venueId) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(Get(venueId).ExteriorPath);

        public static GameObject LoadProp(string venueId, int index)
        {
            var entry = Get(venueId);
            if (index < 0 || index >= entry.PropPaths.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return AssetDatabase.LoadAssetAtPath<GameObject>(entry.PropPaths[index]);
        }
    }
}
