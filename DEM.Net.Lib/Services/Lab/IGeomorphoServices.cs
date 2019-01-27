namespace DEM.Net.Lib.Services.Lab
{
    public interface IGeomorphoServices
    {
        enum_qualificationMorpho_arc GetQualificationMorphoDeLArc(BeanArc_internal p_arc);
        //void SetLignesCretesEtTalwegByRefByArc(BeanTopologieFacettes p_topologieFacette, string p_codeArcATraiter);
    }
}