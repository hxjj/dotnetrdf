/*******************************************************************************
 * Copyright (c) 2009 TopQuadrant, Inc.
 * All rights reserved.
 *******************************************************************************/

using System;
using VDS.RDF.Query.Spin.SparqlUtil;

namespace VDS.RDF.Query.Spin.Model
{
    /**
     * Represents instances of sp:Command (Queries or Update requests).
     *
     * @author Holger Knublauch
     */

    public interface ICommandResource : IPrintable, IResource
    {
        /**
         * Gets the comment if any has been stored as rdfs:comment.
         * @return the comment or null
         */

        String getComment();
    }
}