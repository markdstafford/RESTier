﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Domain.Submit;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Microsoft.Data.Domain.Conventions
{
    /// <summary>
    /// A conventional change set entry validator.
    /// </summary>
    public class ConventionalChangeSetEntryValidator :
        IChangeSetEntryValidator
    {
        private ConventionalChangeSetEntryValidator()
        {
        }

        /// <summary>
        /// A static instance of conventional change set entry validator.
        /// </summary>
        public static readonly ConventionalChangeSetEntryValidator Instance =
            new ConventionalChangeSetEntryValidator();

        /// <inheritdoc/>
        public Task ValidateEntityAsync(
            SubmitContext context, ChangeSetEntry entry,
            ValidationResults validationResults,
            CancellationToken cancellationToken)
        {
            DataModificationEntry dataModificationEntry = entry as DataModificationEntry;
            if (dataModificationEntry != null)
            {
                object entity = dataModificationEntry.Entity;

                // TODO: should this PropertyDescriptorCollection be cached?
                PropertyDescriptorCollection properties = new AssociatedMetadataTypeTypeDescriptionProvider(entity.GetType())
                    .GetTypeDescriptor(entity).GetProperties();

                ValidationContext validationContext = new ValidationContext(entity);

                foreach (PropertyDescriptor property in properties)
                {
                    validationContext.MemberName = property.Name;

                    foreach (ValidationAttribute validationAttribute in property.Attributes.OfType<ValidationAttribute>())
                    {
                        object value = property.GetValue(entity);
                        DataAnnotations.ValidationResult validationResult = validationAttribute.GetValidationResult(value, validationContext);
                        if (validationResult != DataAnnotations.ValidationResult.Success)
                        {
                            validationResults.Add(new Submit.ValidationResult()
                            {
                                Id = validationAttribute.GetType().FullName,
                                Message = validationResult.ErrorMessage,
                                Severity = ValidationSeverity.Error,
                                Target = entity,
                                PropertyName = property.Name
                            });
                        }
                    }
                }
            }

            return Task.WhenAll();
        }
    }
}
