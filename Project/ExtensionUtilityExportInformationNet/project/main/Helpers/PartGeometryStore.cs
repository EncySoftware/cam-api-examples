using CAMAPI.GeomLibrary;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using System.Collections.Generic;
using STTypes;
using System;
using System.Runtime.InteropServices;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Represents a key for a part geometry with two components: SetupStageIndex and PartIndex.
    /// </summary>
    public readonly struct PartKey : IEquatable<PartKey>
    {
        /// <summary>
        /// SetupIdx of part geometry 
        /// </summary>
        public int SetupStageIndex { get; }

        /// <summary>
        /// PartIdx of part geometry 
        /// </summary>
        public int PartIndex { get; }

        /// <summary>
        /// Constructor of part key 
        /// </summary>
        public PartKey(int setupStageIdx, int partIdx)
        {
            SetupStageIndex = setupStageIdx;
            PartIndex = partIdx;
        }

        /// <summary>
        /// Overrides the Equals method to compare two PartKey objects based on their SetupStageIndex and PartIndex properties.
        /// </summary>
        /// <param name="other">The PartKey object to compare with the current object.</param>
        /// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(PartKey other) =>
            SetupStageIndex == other.SetupStageIndex && PartIndex == other.PartIndex;

        /// <summary>
        /// Overrides the Equals method for the object class to compare a PartKey object with another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the current object is equal to the obj parameter; otherwise, false.</returns>
        public override bool Equals(object? obj) =>
            obj is PartKey other && Equals(other);

        /// <summary>
        /// Generates a hash code for the PartKey object based on its SetupStageIndex and PartIndex properties.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() =>
            HashCode.Combine(SetupStageIndex, PartIndex); 

        /// <summary>
        /// Returns a string representation of the PartKey object in the format "SetupStageIndex:PartIndex".
        /// </summary>
        /// <returns>A string representation of the PartKey object.</returns>
        public override string ToString() => $"{SetupStageIndex}:{PartIndex}";

        /// <summary>
        /// Overrides the equality operator to compare two PartKey objects.
        /// </summary>
        /// <param name="left">The first PartKey object to compare.</param>
        /// <param name="right">The second PartKey object to compare.</param>
        /// <returns>True if the left and right parameters are equal; otherwise, false.</returns>
        public static bool operator ==(PartKey left, PartKey right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Overrides the inequality operator to compare two PartKey objects.
        /// </summary>
        /// <param name="left">The first PartKey object to compare.</param>
        /// <param name="right">The second PartKey object to compare.</param>
        /// <returns>True if the left and right parameters are not equal; otherwise, false.</returns>
        public static bool operator !=(PartKey left, PartKey right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Special class for storing part geometry and get access to them by PartKey 
    /// </summary>
    public static class PartGeometryStore
    {
        /// <summary>
        /// Just dictionary 
        /// </summary>
        public static readonly Dictionary<PartKey, PartGeometry> Parts = [];
    }
}