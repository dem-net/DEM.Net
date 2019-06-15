
namespace DEM.Net.Core.FortuneVoronoi
{
	public enum enumTopologieArcAmontAval
	{
		amont,
		aval,
		indifferent,
		nePasTraiter
	}
	//
	public enum enumTopologieExisteAmontAval
	{
		amont,
		aval,
		avalEtAmont,
		nonConnecte
	}
	//
	public enum enumModeAssemblageDesArcs
	{
		entreIntersections
		, global
	}

	public enum enumSensAssemblage
	{
		V1NonInverseV2NonInverseAval,
		V1NonInverseV2NonInverseAmont,
		V1NonInverseV2InverseAval,
		V1NonInverseV2InverseAmont,
		PasConnexion
	}

	public enum enumTopologieDebutFinArc
	{
		debut,
		fin
	}
	public enum enumDebutOuFin
	{
		pointDebut,
		pointFin,
		pointDebutEtFin,
		pointNiDebutNiFin
	}

	public enum enumGaucheOuDroite
	{
		gauche,
		droite
	}

	public enum enumTraitementArc
	{
		NonTraite,
		GaucheTraitee,
		DroiteTraitee,
		DroiteEtGaucheTraitees
	}

	public enum enumNatureDeLArcDansLaTopologieDIlots
	{
		arcBordier,
		arcInclus
	}

	public enum enumQualiteContoursIlot
	{
		IlotSimple,
		IlotAnneau, //Cas de l'anneau extérieur OU des cas types île au milieu du lac
		IlotAvecArcsCordes //Cas des impasses se terminant en boucle
	}
	public enum enumRemonteIlotsParType
	{
		IlotSimple,
		IlotAnneau, //Cas de l'anneau extérieur OU des cas types île au milieu du lac
		IlotAvecArcsCordes, //Cas des impasses se terminant en boucle
		IlotSaufAnneau,
		TousIlots
	}
	public enum enumRenumerotationEtatTraitement
	{
		EnFileTraite,
		EnFileNonTraite,
		HorsFile
	}

	public enum enumMinOuMax
	{
		min,
		max
	}
	public enum enumGetExtension
	{
		horizontale,
		verticale,
		minHorizontaleVerticale,
		maxHorizontaleVerticale
	}

	public enum enumFermeturePolygoneEnPb
	{
		forcerFermeture,
		envoyerException,
		exclurePolygoneSansException
	}

	public enum enumModeProjection
	{
		projectionOrtho,
		projectionDroite,
		nonProjete,
		rattachementVertexProche
	}

	public enum enumMethodeCalculDistance
	{
		cartesianVolOiseau
	}

	public enum enumDecalageDroiteGauche
	{
		decalageDroite,
		decalageGauche,
		pasDecalage
	}
	public enum enumDroiteOuGauche
	{
		droite,
		gauche,
		droiteEtGauche,
		indetermine
	}

	public enum enumProjectionAmontAval
	{
		touteLaDroite,
		interieurDuSegmentDeDroite,
		segmentDeDroitePlusAval,
		segmentDeDroitePlusAmont,
		seulementAvalDuSegmentDeDroite,
		seulementAmontDusegmentDeDroite
	}

	public enum enumSensHoraireAntihoraire
	{
		sensHoraire,
		sensAntihoraire
	}

	public enum enumDecoupageOuEmboutissage
	{
		decoupage,
		inclusionStricte,
		exclusionStricte
	}

	public enum enumMethodeRepositionnementCentroide
	{
		nePasRepositionner,
		forcerDansSurfaceLaPlusProche
	}

	public enum enumMesureDispersion
	{
		plusGrandAxe = 0,
		ecartTypeConvex = 1
	}

	public enum enumMethodeThematique
	{
		memeNbreObjets = 0
	}

	public enum enumCouleurDominante
	{
		red = 0,
		green = 1,
		blue = 2,
		redEtGreen = 3,
		redEtBlue = 4,
		greenEtBlue = 5,
		redEtGreenEtBlue = 6,
		redVersGreen=7,
		greenVersRed=8
	}

	public enum enumQualifArcDelaunay
	{
		bordure,
		interne,
		externe,
		indetermine
	}
	public enum enumSuivantPrecedent
	{
		suivant,
		precedent
	}
	public enum enumAnneauInterieurOuExterieur
	{
		anneauInterieur = 0,
		anneauExterieur = 1
	}
	public enum enumCreuxPlein
	{
		indetermine = 0,
		creux = 1,
		plein = 2
	}

	public enum enumMethodeCah
	{
		objetsLesPlusProches=0
	}
	public enum enumEtatChevauchement
	{
		strictementInclus,
		strictementExclus,
		chevauchantOuTouchant,
		indetermine

	}
}
