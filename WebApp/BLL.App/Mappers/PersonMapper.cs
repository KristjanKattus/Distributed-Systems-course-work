using AutoMapper;

namespace BLL.App.Mappers
{
    public class PersonMapper : BaseMapper<BLL.App.DTO.Person, DAL.App.DTO.Person>
    {
        public PersonMapper(IMapper mapper) : base(mapper)
        {
        }
    }
}