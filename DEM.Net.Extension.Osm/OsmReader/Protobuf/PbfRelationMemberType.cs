//---------------------------------------------------------------------
//---------------------------------------------------------------------
// Library/Code is from http://spatial.litesolutions.net
//---------------------------------------------------------------------
//---------------------------------------------------------------------

namespace DEM.Net.Extension.Osm.Protobuf
{
    /// <summary>
    ///     Defines possible types of the relation member.
    /// </summary>
    public enum PbfRelationMemberType
    {
        /// <summary>
        ///     Relation member is Node.
        /// </summary>
        Node,

        /// <summary>
        ///     Relation member is Way.
        /// </summary>
        Way,

        /// <summary>
        ///     Relation member is Relation.
        /// </summary>
        Relation
    }
}