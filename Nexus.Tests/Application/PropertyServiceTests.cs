using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Application.Interfaces;
using Xunit;
using Nexus.Application.ReadModels;
using Nexus.Application.Interfaces.Repository;

namespace Nexus.Tests.Application
{
    public class PropertyServiceTests
    {
        private readonly Mock<IPropertyRepository> _propertyRepository = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly PropertyService _service;

        public PropertyServiceTests()
        {
            _service = new PropertyService(_propertyRepository.Object, _uow.Object);
        }
    }
}
