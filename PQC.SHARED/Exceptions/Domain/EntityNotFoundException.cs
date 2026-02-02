using PQC.SHARED.Exceptions.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.SHARED.Exceptions.Domain
{
    /// <summary>
    /// Exceção para entidades não encontradas.
    /// </summary>
    public class EntityNotFoundException : BaseException
    {
        public EntityNotFoundException(string message)
            : base(message, "ENTITY_NOT_FOUND", 404)
        {
        }

        public EntityNotFoundException(string entityName, object id)
            : base($"{entityName} with ID '{id}' was not found", "ENTITY_NOT_FOUND", 404)
        {
        }
    }

}
