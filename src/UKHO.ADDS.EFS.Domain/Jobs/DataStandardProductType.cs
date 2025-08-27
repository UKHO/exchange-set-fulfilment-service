using System.ComponentModel.DataAnnotations;

namespace UKHO.ADDS.EFS.Jobs
{
    public enum DataStandardProductType
    {
        [Display(Name = "S-57 (ENC)", Description = "Electronic Navigational chart (IHO S-57 format)")]
        S57 = 0,

        [Display(Name = "S-101 (ENC)", Description = "Navigational chart in (IHO S-101 format)")]
        S101 = 101,

        [Display(Name = "S-102", Description = "Bathymetric Surface")]
        S102 = 102,

        [Display(Name = "S-104", Description = "Water Level Information")]
        S104 = 104,

        [Display(Name = "S-111", Description = "Surface Currents")]
        S111 = 111,

        [Display(Name = "S-121", Description = "Maritime Limits and Boundaries")]
        S121 = 121,

        [Display(Name = "S-122", Description = "Marine Protection Areas")]
        S122 = 122,

        [Display(Name = "S-124", Description = "Navigational Warnings")]
        S124 = 124,

        [Display(Name = "S-125", Description = "Marine Aids to Navigation")]
        S125 = 125,

        [Display(Name = "S-126", Description = "Marine Physical Environment")]
        S126 = 126,

        [Display(Name = "S-127", Description = "Marine Traffic Management")]
        S127 = 127,

        [Display(Name = "S-128", Description = "Catalogue of Nautical Products")]
        S128 = 128,

        [Display(Name = "S-129", Description = "Under Keel Clearance Management")]
        S129 = 129,

        [Display(Name = "S-130", Description = "Polygonal Demarcations of Global Sea Areas")]
        S130 = 130,

        [Display(Name = "S-131", Description = "Marine Harbour Infrastructure")]
        S131 = 131,

        [Display(Name = "S-164", Description = "IHO Test Data Sets for S-100 ECDIS")]
        S164 = 164,
    }
}
