let charts = {};
let currentChartType = 'line';

// Initialize function that will be called from the view
function initializeDashboard(modelData) {
    // Store the data globally
    window.dashboardData = modelData;

    // Initialize all charts
    loadAllCharts();

    // Initialize DataTables
    $('table[id^="dataTable-"]').DataTable({
        pageLength: 25,
        order: [[5, 'desc']],
        responsive: true
    });
}

function changeChartType(type) {
    currentChartType = type;
    $('.btn-group .btn').removeClass('active');
    $(event.target).closest('.btn').addClass('active');
    updateAllCharts();
}

function loadAllCharts() {
    // Use AJAX calls to get properly formatted data
    loadPipelineChart();
    loadTopProgramsChart();
    loadDepartmentChart();
    loadFunnelChart();
    loadComparisonChart();
    loadDepartmentPieCharts();
    loadYearComparisonChart();
    loadIntlDomesticChart();
}

function loadPipelineChart() {
    $.ajax({
        url: '/Dashboard/GetChartData',
        type: 'POST',
        data: { chartType: 'enrollment-trend' },
        success: function (data) {
            const ctx = document.getElementById('pipelineChart');
            if (!ctx) return;

            if (charts.pipeline) charts.pipeline.destroy();

            charts.pipeline = new Chart(ctx.getContext('2d'), {
                type: currentChartType === 'pie' || currentChartType === 'doughnut' ? 'bar' : currentChartType,
                data: data,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: {
                            display: true,
                            text: 'Enrollment Pipeline by Year'
                        },
                        legend: {
                            display: true,
                            position: 'top'
                        }
                    },
                    scales: currentChartType !== 'radar' ? {
                        y: {
                            beginAtZero: true
                        }
                    } : {}
                }
            });
        }
    });
}

function loadTopProgramsChart() {
    $.ajax({
        url: '/Dashboard/GetChartData',
        type: 'POST',
        data: { chartType: 'top-programs' },
        success: function (data) {
            const ctx = document.getElementById('topProgramsChart');
            if (!ctx) return;

            if (charts.topPrograms) charts.topPrograms.destroy();

            charts.topPrograms = new Chart(ctx.getContext('2d'), {
                type: 'bar',
                data: data,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    indexAxis: 'y',
                    scales: {
                        x: {
                            beginAtZero: true
                        }
                    },
                    plugins: {
                        title: {
                            display: true,
                            text: 'Top 10 Programs by Registration'
                        }
                    }
                }
            });
        }
    });
}

function loadDepartmentChart() {
    const coeData = window.dashboardData.coeData;
    const cosData = window.dashboardData.cosData;

    const ctx = document.getElementById('departmentChart');
    if (!ctx) return;

    if (charts.department) charts.department.destroy();

    charts.department = new Chart(ctx.getContext('2d'), {
        type: 'doughnut',
        data: {
            labels: ['COE', 'COS'],
            datasets: [{
                data: [coeData.length, cosData.length],
                backgroundColor: ['#3498db', '#2ecc71']
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                title: {
                    display: true,
                    text: 'Programs by Department'
                }
            }
        }
    });
}

function loadFunnelChart() {
    $.ajax({
        url: '/Dashboard/GetChartData',
        type: 'POST',
        data: { chartType: 'pipeline-funnel' },
        success: function (data) {
            const ctx = document.getElementById('funnelChart');
            if (!ctx) return;

            if (charts.funnel) charts.funnel.destroy();

            charts.funnel = new Chart(ctx.getContext('2d'), {
                type: 'bar',
                data: data,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    indexAxis: 'y',
                    plugins: {
                        title: {
                            display: true,
                            text: 'Enrollment Funnel by Year'
                        }
                    },
                    scales: {
                        x: {
                            beginAtZero: true
                        }
                    }
                }
            });
        }
    });
}

function loadComparisonChart() {
    $.ajax({
        url: '/Dashboard/GetChartData',
        type: 'POST',
        data: { chartType: 'department-comparison' },
        success: function (data) {
            const ctx = document.getElementById('comparisonChart');
            if (!ctx) return;

            if (charts.comparison) charts.comparison.destroy();

            charts.comparison = new Chart(ctx.getContext('2d'), {
                type: 'bar',
                data: data,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: {
                            display: true,
                            text: 'Department Comparison'
                        }
                    }
                }
            });
        }
    });
}

function loadDepartmentPieCharts() {
    // COE Pie Chart
    $.ajax({
        url: '/Dashboard/GetChartData',
        type: 'POST',
        data: { chartType: 'international-domestic', department: 'COE' },
        success: function (data) {
            const ctx = document.getElementById('coePieChart');
            if (!ctx) return;

            if (charts.coePie) charts.coePie.destroy();

            charts.coePie = new Chart(ctx.getContext('2d'), {
                type: 'pie',
                data: data,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: {
                            display: true,
                            text: 'COE: International vs Domestic'
                        }
                    }
                }
            });
        }
    });

    // COS Pie Chart
    $.ajax({
        url: '/Dashboard/GetChartData',
        type: 'POST',
        data: { chartType: 'international-domestic', department: 'COS' },
        success: function (data) {
            const ctx = document.getElementById('cosPieChart');
            if (!ctx) return;

            if (charts.cosPie) charts.cosPie.destroy();

            charts.cosPie = new Chart(ctx.getContext('2d'), {
                type: 'pie',
                data: data,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: {
                            display: true,
                            text: 'COS: International vs Domestic'
                        }
                    }
                }
            });
        }
    });
}

function loadYearComparisonChart() {
    const ctx = document.getElementById('yearComparisonChart');
    if (!ctx) return;

    const coeData = window.dashboardData.coeData;
    const cosData = window.dashboardData.cosData;

    if (charts.yearComparison) charts.yearComparison.destroy();

    charts.yearComparison = new Chart(ctx.getContext('2d'), {
        type: 'line',
        data: {
            labels: ['2023', '2024', '2025'],
            datasets: [
                {
                    label: 'COE Registered',
                    data: [
                        coeData.reduce((sum, d) => sum + (d.registered2023 || 0), 0),
                        coeData.reduce((sum, d) => sum + (d.registered2024 || 0), 0),
                        coeData.reduce((sum, d) => sum + (d.registered2025 || 0), 0)
                    ],
                    borderColor: '#3498db',
                    backgroundColor: 'rgba(52, 152, 219, 0.2)'
                },
                {
                    label: 'COS Registered',
                    data: [
                        cosData.reduce((sum, d) => sum + (d.registered2023 || 0), 0),
                        cosData.reduce((sum, d) => sum + (d.registered2024 || 0), 0),
                        cosData.reduce((sum, d) => sum + (d.registered2025 || 0), 0)
                    ],
                    borderColor: '#2ecc71',
                    backgroundColor: 'rgba(46, 204, 113, 0.2)'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                title: {
                    display: true,
                    text: 'Year-over-Year Registration Trends'
                }
            }
        }
    });
}

function loadIntlDomesticChart() {
    $.ajax({
        url: '/Dashboard/GetChartData',
        type: 'POST',
        data: { chartType: 'international-domestic', department: 'All' },
        success: function (data) {
            const ctx = document.getElementById('intlDomesticChart');
            if (!ctx) return;

            if (charts.intlDomestic) charts.intlDomestic.destroy();

            charts.intlDomestic = new Chart(ctx.getContext('2d'), {
                type: 'doughnut',
                data: data,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: {
                            display: true,
                            text: 'Overall International vs Domestic Distribution'
                        }
                    }
                }
            });
        }
    });
}

function updateAllCharts() {
    loadAllCharts();
}

function applyFilters() {
    document.getElementById('filterForm').submit();
}

// Export function
function exportData(format) {
    window.location.href = '/Dashboard/ExportData?format=' + format;
}