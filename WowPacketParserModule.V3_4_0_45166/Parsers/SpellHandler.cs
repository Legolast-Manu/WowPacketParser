﻿using System.Collections.Generic;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.PacketStructures;
using WowPacketParser.Parsing;
using WowPacketParser.Proto;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V3_4_0_45166.Parsers
{
    public static class SpellHandler
    {
        public static void ReadSpellCastVisual(Packet packet, params object[] indexes)
        {
            packet.ReadInt32("SpellXSpellVisualID", indexes);
            packet.ReadInt32("ScriptVisualID", indexes);
        }

        public static void ReadOptionalReagent(Packet packet, params object[] indexes)
        {
            packet.ReadInt32<ItemId>("ItemID", indexes);
            packet.ReadInt32("Slot", indexes);
        }

        public static void ReadOptionalCurrency(Packet packet, params object[] indexes)
        {
            packet.ReadInt32<ItemId>("ItemID", indexes);
            packet.ReadInt32("Slot", indexes);
            packet.ReadInt32("Count", indexes);
        }

        public static uint ReadSpellCastRequest(Packet packet, params object[] idx)
        {
            packet.ReadPackedGuid128("CastID", idx);

            for (var i = 0; i < 2; i++)
                packet.ReadInt32("Misc", idx, i);

            var spellId = packet.ReadUInt32<SpellId>("SpellID", idx);
            packet.ReadInt32("SpellXSpellVisual", idx);

            V6_0_2_19033.Parsers.SpellHandler.ReadMissileTrajectoryRequest(packet, idx, "MissileTrajectory");

            packet.ReadPackedGuid128("Guid", idx);

            var optionalReagentCount = packet.ReadUInt32("OptionalReagentCount", idx);
            var optionalCurrenciesCount = packet.ReadUInt32("OptionalCurrenciesCount", idx);

            for (var i = 0; i < optionalReagentCount; ++i)
                ReadOptionalReagent(packet, idx, "OptionalReagent", i);

            for (var j = 0; j < optionalCurrenciesCount; ++j)
                ReadOptionalCurrency(packet, idx, "OptionalCurrency", j);


            packet.ResetBitReader();
            packet.ReadBits("SendCastFlags", 5, idx);
            var hasMoveUpdate = packet.ReadBit("HasMoveUpdate", idx);

            var weightCount = packet.ReadBits("WeightCount", 2, idx);

            V8_0_1_27101.Parsers.SpellHandler.ReadSpellTargetData(packet, null, spellId, idx, "Target");

            if (hasMoveUpdate)
                V7_0_3_22248.Parsers.MovementHandler.ReadMovementStats(packet, idx, "MoveUpdate");

            for (var i = 0; i < weightCount; ++i)
                V6_0_2_19033.Parsers.SpellHandler.ReadSpellWeight(packet, idx, "Weight", i);

            return spellId;
        }

        public static void ReadSpellCastData(Packet packet, params object[] idx)
        {
            var dbdata = new PacketSpellData();
            packet.ReadPackedGuid128("CasterGUID", idx);
            dbdata.Caster = packet.ReadPackedGuid128("CasterUnit", idx);

            packet.ReadPackedGuid128("CastID", idx);
            packet.ReadPackedGuid128("OriginalCastID", idx);

            dbdata.Spell = packet.ReadUInt32<SpellId>("SpellID", idx);
            packet.ReadUInt32("SpellXSpellVisualID", idx);

            dbdata.Flags = packet.ReadUInt32("CastFlags", idx);
            dbdata.Flags2 = packet.ReadUInt32("CastFlagsEx", idx);
            dbdata.CastTime = packet.ReadUInt32("CastTime", idx);

            V6_0_2_19033.Parsers.SpellHandler.ReadMissileTrajectoryResult(packet, idx, "MissileTrajectory");

            packet.ReadByte("DestLocSpellCastIndex", idx);

            V6_0_2_19033.Parsers.SpellHandler.ReadCreatureImmunities(packet, idx, "Immunities");

            V6_0_2_19033.Parsers.SpellHandler.ReadSpellHealPrediction(packet, idx, "Predict");

            packet.ResetBitReader();

            var hitTargetsCount = packet.ReadBits("HitTargetsCount", 16, idx);
            var missTargetsCount = packet.ReadBits("MissTargetsCount", 16, idx);
            var missStatusCount = packet.ReadBits("MissStatusCount", 16, idx);
            var remainingPowerCount = packet.ReadBits("RemainingPowerCount", 9, idx);

            var hasRuneData = packet.ReadBit("HasRuneData", idx);
            var targetPointsCount = packet.ReadBits("TargetPointsCount", 16, idx);
            var hasAmmoDisplayId = packet.ReadBit("HasAmmoDisplayId", idx);
            var hasAmmoInventoryType = packet.ReadBit("HasAmmoInventoryType", idx);

            for (var i = 0; i < missStatusCount; ++i)
                V6_0_2_19033.Parsers.SpellHandler.ReadSpellMissStatus(packet, idx, "MissStatus", i);

            V8_0_1_27101.Parsers.SpellHandler.ReadSpellTargetData(packet, dbdata, dbdata.Spell, idx, "Target");

            for (var i = 0; i < hitTargetsCount; ++i)
                dbdata.HitTargets.Add(packet.ReadPackedGuid128("HitTarget", idx, i));

            for (var i = 0; i < missTargetsCount; ++i)
                dbdata.MissedTargets.Add(packet.ReadPackedGuid128("MissTarget", idx, i));

            for (var i = 0; i < remainingPowerCount; ++i)
                V6_0_2_19033.Parsers.SpellHandler.ReadSpellPowerData(packet, idx, "RemainingPower", i);

            if (hasRuneData)
                V7_0_3_22248.Parsers.SpellHandler.ReadRuneData(packet, idx, "RemainingRunes");

            for (var i = 0; i < targetPointsCount; ++i)
                V6_0_2_19033.Parsers.SpellHandler.ReadLocation(packet, idx, "TargetPoints", i);

            if (hasAmmoDisplayId)
                dbdata.AmmoDisplayId = packet.ReadInt32("AmmoDisplayId", idx);

            if (hasAmmoInventoryType)
                dbdata.AmmoInventoryType = (uint)packet.ReadInt32E<InventoryType>("InventoryType", idx);
        }

        [Parser(Opcode.CMSG_CAST_SPELL)]
        public static void HandleCastSpell(Packet packet)
        {
            ReadSpellCastRequest(packet, "Cast");
        }

        [Parser(Opcode.SMSG_CAST_FAILED)]
        public static void HandleCastFailed(Packet packet)
        {
            var spellFail = packet.Holder.SpellCastFailed = new();
            spellFail.CastGuid = packet.ReadPackedGuid128("CastID");
            spellFail.Spell = (uint)packet.ReadInt32<SpellId>("SpellID");
            packet.ReadInt32("SpellXSpellVisual");
            spellFail.Success = packet.ReadInt32("Reason") == 0;
            packet.ReadInt32("FailedArg1");
            packet.ReadInt32("FailedArg2");
        }

        [Parser(Opcode.SMSG_SPELL_START)]
        public static void HandleSpellStart(Packet packet)
        {
            ReadSpellCastData(packet, "Cast");
        }

        [Parser(Opcode.SMSG_SPELL_GO)]
        public static void HandleSpellGo(Packet packet)
        {
            ReadSpellCastData(packet, "Cast");

            packet.ResetBitReader();
            var hasLog = packet.ReadBit();
            if (hasLog)
                V8_0_1_27101.Parsers.SpellHandler.ReadSpellCastLogData(packet, "LogData");
        }

        [Parser(Opcode.SMSG_SPELL_FAILED_OTHER)]
        public static void HandleSpellFailedOther(Packet packet)
        {
            var spellFail = packet.Holder.SpellFailure = new();
            spellFail.Caster = packet.ReadPackedGuid128("CasterUnit");
            spellFail.CastGuid = packet.ReadPackedGuid128("CastID");
            spellFail.Spell = packet.ReadUInt32<SpellId>("SpellID");
            packet.ReadInt32("SpellXSpellVisual");
            spellFail.Success = packet.ReadByte("Reason") == 0;
        }

        [Parser(Opcode.SMSG_SPELL_FAILURE)]
        public static void HandleSpellFailure(Packet packet)
        {
            var spellFail = packet.Holder.SpellFailure = new();
            spellFail.Caster = packet.ReadPackedGuid128("CasterUnit");
            spellFail.CastGuid = packet.ReadPackedGuid128("CastID");
            spellFail.Spell = (uint)packet.ReadInt32<SpellId>("SpellID");
            packet.ReadInt32("SpellXSpellVisual");
            spellFail.Success = packet.ReadInt16("Reason") == 0;
        }

        [Parser(Opcode.SMSG_SPELL_CHANNEL_START)]
        public static void HandleSpellChannelStart(Packet packet)
        {
            packet.ReadPackedGuid128("CasterGUID");
            packet.ReadInt32<SpellId>("SpellID");
            packet.ReadInt32("SpellXSpellVisual");
            packet.ReadInt32("ChannelDuration");

            var hasInterruptImmunities = packet.ReadBit("HasInterruptImmunities");
            var hasHealPrediction = packet.ReadBit("HasHealPrediction");

            if (hasInterruptImmunities)
                V6_0_2_19033.Parsers.SpellHandler.ReadSpellChannelStartInterruptImmunities(packet, "InterruptImmunities");

            if (hasHealPrediction)
                V6_0_2_19033.Parsers.SpellHandler.ReadSpellTargetedHealPrediction(packet, "HealPrediction");
        }
    }
}
