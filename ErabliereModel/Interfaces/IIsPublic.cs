using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErabliereApi.Donnees.Interfaces;
public interface IIsPublic
{
    /// <summary>
    /// Indique si l'objet est public
    /// </summary>
    public bool IsPublic { get; set; }
}
