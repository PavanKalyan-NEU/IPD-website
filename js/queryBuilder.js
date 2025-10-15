document.addEventListener('DOMContentLoaded', function () {
    let criteriaCount = 1;
    const categorySelect = document.getElementById('PrimaryCategory');
    const queryTemplateSelect = document.getElementById('queryTemplate');
    const criteriaContainer = document.getElementById('criteriaContainer');
    const outputFieldsContainer = document.getElementById('outputFieldsContainer');

    // Handle category change
    if (categorySelect) {
        categorySelect.addEventListener('change', function () {
            const category = this.value;
            if (category) {
                loadFieldsForCategory(category);
            }
        });
    }

    // Handle template selection
    if (queryTemplateSelect) {
        queryTemplateSelect.addEventListener('change', function () {
            const templateId = this.value;
            if (templateId) {
                loadQueryTemplate(templateId);
            }
        });
    }

    // Add criteria button
    const addCriteriaBtn = document.getElementById('addCriteria');
    if (addCriteriaBtn) {
        addCriteriaBtn.addEventListener('click', function () {
            addCriteriaRow();
        });
    }

    // Load fields for category
    function loadFieldsForCategory(category) {
        fetch(`/Query/GetFieldsForCategory?category=${category}`)
            .then(response => response.json())
            .then(fields => {
                updateFieldSelects(fields);
                updateOutputFields(fields);
            });
    }

    // Load query template
    function loadQueryTemplate(templateId) {
        fetch(`/Query/GetQueryTemplate?templateId=${templateId}`)
            .then(response => response.json())
            .then(template => {
                if (template) {
                    // Set category
                    categorySelect.value = template.category;
                    categorySelect.dispatchEvent(new Event('change'));

                    // Clear and populate criteria
                    criteriaContainer.innerHTML = '';
                    criteriaCount = 0;

                    template.defaultCriteria.forEach(criteria => {
                        addCriteriaRow(criteria);
                    });

                    // Set recommended fields
                    setTimeout(() => {
                        template.recommendedFields.forEach(field => {
                            const checkbox = document.querySelector(`input[value="${field}"]`);
                            if (checkbox) checkbox.checked = true;
                        });
                    }, 500);
                }
            });
    }

    // Add criteria row
    function addCriteriaRow(criteria = null) {
        const row = document.createElement('div');
        row.className = 'criteria-row mb-2';
        row.innerHTML = `
            <div class="row">
                <div class="col-md-3">
                    <select name="SearchCriteria[${criteriaCount}].Field" class="form-select field-select">
                        <option value="">-- Select Field --</option>
                    </select>
                </div>
                <div class="col-md-3">
                    <select name="SearchCriteria[${criteriaCount}].Operator" class="form-select">
                        <option value="contains">Contains</option>
                        <option value="equals">Equals</option>
                        <option value="not equals">Not Equals</option>
                        <option value="greater than">Greater Than</option>
                        <option value="less than">Less Than</option>
                    </select>
                </div>
                <div class="col-md-4">
                    <input type="text" name="SearchCriteria[${criteriaCount}].Value" 
                           class="form-control" placeholder="Value" />
                </div>
                <div class="col-md-2">
                    <div class="input-group">
                        <select name="SearchCriteria[${criteriaCount}].LogicalOperator" class="form-select">
                            <option value="AND">AND</option>
                            <option value="OR">OR</option>
                        </select>
                        <button type="button" class="btn btn-danger btn-sm remove-criteria">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;

        criteriaContainer.appendChild(row);

        // Add remove handler
        row.querySelector('.remove-criteria').addEventListener('click', function () {
            row.remove();
        });

        // Update field options
        const currentCategory = categorySelect.value;
        if (currentCategory) {
            loadFieldsForCategory(currentCategory);
        }

        // Set values if criteria provided
        if (criteria) {
            setTimeout(() => {
                row.querySelector('[name$=".Field"]').value = criteria.field;
                row.querySelector('[name$=".Operator"]').value = criteria.operator;
                row.querySelector('[name$=".Value"]').value = criteria.value;
                row.querySelector('[name$=".LogicalOperator"]').value = criteria.logicalOperator || 'AND';
            }, 100);
        }

        criteriaCount++;
    }

    // Update field selects
    function updateFieldSelects(fields) {
        document.querySelectorAll('.field-select').forEach(select => {
            const currentValue = select.value;
            select.innerHTML = '<option value="">-- Select Field --</option>';
            fields.forEach(field => {
                const option = document.createElement('option');
                option.value = field;
                option.textContent = field.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
                select.appendChild(option);
            });
            if (currentValue) select.value = currentValue;
        });
    }

    // Update output fields
    function updateOutputFields(fields) {
        outputFieldsContainer.innerHTML = '';
        fields.forEach((field, index) => {
            const div = document.createElement('div');
            div.className = 'col-md-4 mb-2';
            div.innerHTML = `
                <div class="form-check">
                    <input class="form-check-input" type="checkbox" 
                           name="SelectedOutputFields" value="${field}" 
                           id="field_${index}">
                    <label class="form-check-label" for="field_${index}">
                        ${field.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase())}
                    </label>
                </div>
            `;
            outputFieldsContainer.appendChild(div);
        });
    }

    // Initialize with first criteria row if empty
    if (criteriaContainer && criteriaContainer.children.length === 0) {
        addCriteriaRow();
    }
});