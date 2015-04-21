/*******************************************************************************
 * Copyright (c) 2009 TopQuadrant, Inc.
 * All rights reserved.
 *******************************************************************************/

namespace VDS.RDF.Query.Spin.Model
{
    /**
     * Abstract base interface of TemplateCall and FunctionCall.
     *
     * @author Holger Knublauch
     */

    public interface IModuleCallResource : IResource
    {
        /**
         * Gets the associated module, i.e. SPIN function or template, derived from the rdf:type.
         * @return the module
         */

        IModuleResource getModule();
    }
}