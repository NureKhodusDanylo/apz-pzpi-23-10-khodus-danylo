using Application.DTOs.FileDTOs;

namespace RobDeliveryAPI.Extensions
{
    public static class FormFileExtensions
    {
        public static FileUploadDTO ToFileUploadDTO(this IFormFile formFile)
        {
            return new FileUploadDTO
            {
                FileName = formFile.FileName,
                ContentType = formFile.ContentType,
                Length = formFile.Length,
                Content = formFile.OpenReadStream()
            };
        }

        public static List<FileUploadDTO> ToFileUploadDTOs(this IFormFileCollection formFiles)
        {
            return formFiles.Select(f => f.ToFileUploadDTO()).ToList();
        }
    }
}
