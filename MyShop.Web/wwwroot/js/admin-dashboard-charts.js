/**
 * داشبورد ادمین — چارت‌ها با ECharts لوکال (Gentelella)
 * اولویت: فروش → سفارش → کوپن → جعبه‌شانس
 */
(function () {
    'use strict';

    var PRIMARY = '#1abb9c';
    var BLUE = '#066fd1';
    var GREEN = '#2fb344';
    var AMBER = '#f59f00';
    var RED = '#d63939';
    var PURPLE = '#ae3ec9';
    var CYAN = '#17a2b8';

    function ready() {
        return typeof echarts !== 'undefined';
    }

    function ensureHost(id, minHeight) {
        var el = document.getElementById(id);
        if (!el) return null;
        if (el.tagName === 'CANVAS') {
            var div = document.createElement('div');
            div.id = id;
            div.className = (el.className || '') + ' echart-host';
            var h = minHeight || 280;
            div.style.cssText = 'width:100%;height:' + h + 'px;min-height:' + h + 'px;';
            if (el.parentNode) el.parentNode.replaceChild(div, el);
            return div;
        }
        if (!el.style.height) el.style.height = (minHeight || 280) + 'px';
        if (!el.style.width) el.style.width = '100%';
        return el;
    }

    function dispose(key) {
        var inst = window[key];
        if (inst && typeof inst.dispose === 'function') {
            try { inst.dispose(); } catch (e) { /* ignore */ }
        }
        window[key] = null;
    }

    function baseTextStyle() {
        return { fontFamily: 'Vazirmatn, Tahoma, sans-serif' };
    }

    function formatMoney(n) {
        var v = Math.round(Number(n) || 0);
        return v.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',') + ' تومان';
    }

    function formatNum(n) {
        return Math.round(Number(n) || 0).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    window.checkChartJsLoaded = function () { return ready(); };
    window.checkCanvasExists = function (id) { return document.getElementById(id) !== null; };
    window.checkChartInstanceExists = function () { return !!(window.salesTrendChartInstance); };
    window.showCanvas = function (id) {
        var el = document.getElementById(id);
        if (!el) return false;
        el.style.visibility = 'visible';
        el.style.position = 'relative';
        el.style.display = 'block';
        return true;
    };

    function mount(id, instanceKey, option, height) {
        if (!ready()) {
            console.error('ECharts is not loaded (expect /gentelella/js/echarts.min.js)');
            return false;
        }
        var host = ensureHost(id, height || 280);
        if (!host) {
            console.error('Chart host not found:', id);
            return false;
        }
        dispose(instanceKey);
        var chart = echarts.init(host, null, { renderer: 'canvas' });
        chart.setOption(option);
        window[instanceKey] = chart;
        setTimeout(function () { try { chart.resize(); } catch (e) { /* ignore */ } }, 80);
        return true;
    }

    /** فروش روزانه — خطی ناحیه‌ای با مبلغ */
    window.renderSalesTrendChart = function (labels, values) {
        return mount('salesTrendChart', 'salesTrendChartInstance', {
            textStyle: baseTextStyle(),
            color: [PRIMARY],
            tooltip: {
                trigger: 'axis',
                formatter: function (params) {
                    var p = params && params[0];
                    if (!p) return '';
                    return p.name + '<br/>' + p.marker + ' فروش: ' + formatMoney(p.value);
                }
            },
            grid: { left: 56, right: 20, top: 28, bottom: 48 },
            xAxis: {
                type: 'category',
                data: labels || [],
                boundaryGap: false,
                axisLabel: { rotate: 35, fontSize: 10, color: '#7e8896' }
            },
            yAxis: {
                type: 'value',
                axisLabel: {
                    fontSize: 10,
                    color: '#7e8896',
                    formatter: function (v) {
                        if (v >= 1e9) return (v / 1e9).toFixed(1) + 'B';
                        if (v >= 1e6) return (v / 1e6).toFixed(1) + 'M';
                        if (v >= 1e3) return (v / 1e3).toFixed(0) + 'K';
                        return v;
                    }
                },
                splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } }
            },
            series: [{
                name: 'فروش',
                type: 'line',
                smooth: true,
                symbol: 'circle',
                symbolSize: 7,
                data: values || [],
                itemStyle: { color: PRIMARY },
                lineStyle: { width: 3, color: PRIMARY },
                areaStyle: {
                    color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                        { offset: 0, color: 'rgba(26,187,156,0.35)' },
                        { offset: 1, color: 'rgba(26,187,156,0.02)' }
                    ])
                }
            }]
        }, 320);
    };

    /** سهم فروش عادی / کوپن / جعبه‌شانس */
    window.renderSalesMixChart = function (labels, values) {
        return mount('salesMixChart', 'salesMixChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: {
                trigger: 'item',
                formatter: function (p) {
                    return p.name + '<br/>' + formatMoney(p.value) + ' (' + (p.percent || 0) + '%)';
                }
            },
            legend: { bottom: 0, textStyle: { fontSize: 12 } },
            series: [{
                type: 'pie',
                radius: ['48%', '72%'],
                center: ['50%', '45%'],
                avoidLabelOverlap: true,
                label: { show: false },
                data: (labels || []).map(function (l, i) {
                    return {
                        name: l,
                        value: (values && values[i] != null) ? values[i] : 0,
                        itemStyle: { color: [BLUE, AMBER, PURPLE][i] || PRIMARY }
                    };
                })
            }]
        }, 300);
    };

    /** وضعیت سفارش‌ها — دونات */
    window.renderOrderStatusChart = function (labels, values) {
        return mount('orderStatusChart', 'orderStatusChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: {
                trigger: 'item',
                formatter: function (p) {
                    return p.name + ': ' + formatNum(p.value) + ' (' + (p.percent || 0) + '%)';
                }
            },
            legend: { bottom: 0, textStyle: { fontSize: 12 } },
            series: [{
                type: 'pie',
                radius: ['45%', '70%'],
                center: ['50%', '45%'],
                label: { show: false },
                data: (labels || []).map(function (l, i) {
                    return {
                        name: l,
                        value: (values && values[i] != null) ? values[i] : 0,
                        itemStyle: { color: [AMBER, GREEN, RED][i] || BLUE }
                    };
                })
            }]
        }, 300);
    };

    /** تعداد سفارش روزانه — ستونی */
    window.renderOrdersDailyChart = function (labels, values) {
        return mount('ordersDailyChart', 'ordersDailyChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: { trigger: 'axis' },
            grid: { left: 40, right: 16, top: 24, bottom: 48 },
            xAxis: {
                type: 'category',
                data: labels || [],
                axisLabel: { rotate: 35, fontSize: 10, color: '#7e8896' }
            },
            yAxis: {
                type: 'value',
                minInterval: 1,
                axisLabel: { fontSize: 11, color: '#7e8896' },
                splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } }
            },
            series: [{
                name: 'سفارش',
                type: 'bar',
                barMaxWidth: 36,
                data: values || [],
                itemStyle: {
                    color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                        { offset: 0, color: BLUE },
                        { offset: 1, color: 'rgba(6,111,209,0.55)' }
                    ]),
                    borderRadius: [6, 6, 0, 0]
                }
            }]
        }, 300);
    };

    /** Top کوپن‌ها — میله‌ای افقی */
    window.renderTopCouponsChart = function (labels, values) {
        var labs = (labels || []).slice().reverse();
        var vals = (values || []).slice().reverse();
        return mount('topCouponsChart', 'topCouponsChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: {
                trigger: 'axis',
                axisPointer: { type: 'shadow' },
                formatter: function (params) {
                    var p = params && params[0];
                    if (!p) return '';
                    return p.name + '<br/>استفاده: ' + formatNum(p.value);
                }
            },
            grid: { left: 100, right: 24, top: 16, bottom: 24 },
            xAxis: {
                type: 'value',
                minInterval: 1,
                axisLabel: { fontSize: 11, color: '#7e8896' },
                splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } }
            },
            yAxis: {
                type: 'category',
                data: labs,
                axisLabel: { fontSize: 11, color: '#626d7d' }
            },
            series: [{
                type: 'bar',
                data: vals,
                barMaxWidth: 22,
                itemStyle: {
                    color: AMBER,
                    borderRadius: [0, 6, 6, 0]
                },
                label: {
                    show: true,
                    position: 'right',
                    fontSize: 11,
                    color: '#626d7d'
                }
            }]
        }, 300);
    };

    /** جعبه‌شانس — فروش روزانه یا مقایسه ساده */
    window.renderChanceBoxChart = function (labels, values) {
        return mount('chanceBoxChart', 'chanceBoxChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: {
                trigger: 'axis',
                formatter: function (params) {
                    var p = params && params[0];
                    if (!p) return '';
                    return p.name + '<br/>' + p.marker + ' ' + formatMoney(p.value);
                }
            },
            grid: { left: 56, right: 20, top: 28, bottom: 48 },
            xAxis: {
                type: 'category',
                data: labels || [],
                axisLabel: { rotate: 35, fontSize: 10, color: '#7e8896' }
            },
            yAxis: {
                type: 'value',
                axisLabel: {
                    fontSize: 10,
                    color: '#7e8896',
                    formatter: function (v) {
                        if (v >= 1e6) return (v / 1e6).toFixed(1) + 'M';
                        if (v >= 1e3) return (v / 1e3).toFixed(0) + 'K';
                        return v;
                    }
                },
                splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } }
            },
            series: [{
                name: 'فروش جعبه‌شانس',
                type: 'bar',
                barMaxWidth: 36,
                data: values || [],
                itemStyle: {
                    color: PURPLE,
                    borderRadius: [6, 6, 0, 0]
                }
            }]
        }, 280);
    };

    /** کاربران — توزیع نقش */
    window.renderUserRolesChart = function (labels, values) {
        return mount('userRolesChart', 'userRolesChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: {
                trigger: 'item',
                formatter: function (p) {
                    return p.name + ': ' + formatNum(p.value) + ' (' + (p.percent || 0) + '%)';
                }
            },
            legend: { bottom: 0, textStyle: { fontSize: 11 } },
            series: [{
                type: 'pie',
                radius: ['42%', '68%'],
                center: ['50%', '44%'],
                label: { show: false },
                data: (labels || []).map(function (l, i) {
                    return {
                        name: l,
                        value: (values && values[i] != null) ? values[i] : 0,
                        itemStyle: { color: [BLUE, GREEN, AMBER, PURPLE, CYAN, RED][i % 6] }
                    };
                })
            }]
        }, 300);
    };

    /** کاربران — ثبت‌نام روزانه */
    window.renderUserRegTrendChart = function (labels, values) {
        return mount('userRegTrendChart', 'userRegTrendChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: { trigger: 'axis' },
            grid: { left: 40, right: 16, top: 24, bottom: 48 },
            xAxis: {
                type: 'category',
                data: labels || [],
                axisLabel: { rotate: 35, fontSize: 10, color: '#7e8896' }
            },
            yAxis: {
                type: 'value',
                minInterval: 1,
                axisLabel: { fontSize: 11, color: '#7e8896' },
                splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } }
            },
            series: [{
                name: 'ثبت‌نام',
                type: 'line',
                smooth: true,
                data: values || [],
                itemStyle: { color: BLUE },
                lineStyle: { width: 2.5, color: BLUE },
                areaStyle: { color: 'rgba(6,111,209,0.15)' }
            }]
        }, 300);
    };

    /** تیکت‌ها — وضعیت */
    window.renderTicketStatusChart = function (labels, values) {
        return mount('ticketStatusChart', 'ticketStatusChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: {
                trigger: 'item',
                formatter: function (p) {
                    return p.name + ': ' + formatNum(p.value);
                }
            },
            legend: { bottom: 0, textStyle: { fontSize: 12 } },
            series: [{
                type: 'pie',
                radius: ['45%', '70%'],
                center: ['50%', '45%'],
                label: { show: false },
                data: (labels || []).map(function (l, i) {
                    return {
                        name: l,
                        value: (values && values[i] != null) ? values[i] : 0,
                        itemStyle: { color: [AMBER, BLUE, GREEN, RED][i] || PRIMARY }
                    };
                })
            }]
        }, 300);
    };

    /** لاگین روزانه — وضعیت سیستم */
    window.renderLoginTrendChart = function (labels, values) {
        return mount('loginTrendChart', 'loginTrendChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: { trigger: 'axis' },
            grid: { left: 40, right: 16, top: 24, bottom: 48 },
            xAxis: {
                type: 'category',
                data: labels || [],
                axisLabel: { rotate: 35, fontSize: 10, color: '#7e8896' }
            },
            yAxis: {
                type: 'value',
                minInterval: 1,
                axisLabel: { fontSize: 11, color: '#7e8896' },
                splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } }
            },
            series: [{
                name: 'ورود یکتا',
                type: 'bar',
                barMaxWidth: 28,
                data: values || [],
                itemStyle: {
                    color: GREEN,
                    borderRadius: [5, 5, 0, 0]
                }
            }]
        }, 280);
    };

    /** پروژه‌ها — وضعیت */
    window.renderProjectStatusChart = function (labels, values) {
        return mount('projectStatusChart', 'projectStatusChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: { trigger: 'item' },
            legend: { bottom: 0, textStyle: { fontSize: 11 } },
            series: [{
                type: 'pie',
                radius: ['42%', '68%'],
                center: ['50%', '44%'],
                label: { show: false },
                data: (labels || []).map(function (l, i) {
                    return {
                        name: l,
                        value: (values && values[i] != null) ? values[i] : 0,
                        itemStyle: { color: [BLUE, PRIMARY, AMBER, GREEN, RED][i % 5] }
                    };
                })
            }]
        }, 300);
    };

    /** وظایف — وضعیت Todo/Doing/Done */
    window.renderProjectTasksChart = function (labels, values) {
        return mount('projectTasksChart', 'projectTasksChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: { trigger: 'axis' },
            grid: { left: 40, right: 16, top: 24, bottom: 40 },
            xAxis: {
                type: 'category',
                data: labels || [],
                axisLabel: { fontSize: 12, color: '#7e8896' }
            },
            yAxis: {
                type: 'value',
                minInterval: 1,
                axisLabel: { fontSize: 11, color: '#7e8896' },
                splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } }
            },
            series: [{
                type: 'bar',
                barMaxWidth: 48,
                data: (values || []).map(function (v, i) {
                    return {
                        value: v,
                        itemStyle: {
                            color: [AMBER, BLUE, GREEN][i] || PRIMARY,
                            borderRadius: [6, 6, 0, 0]
                        }
                    };
                })
            }]
        }, 300);
    };

    /** فازها — تعداد وظیفه در هر فاز (افقی) */
    window.renderProjectPhasesChart = function (labels, values) {
        var labs = (labels || []).slice().reverse();
        var vals = (values || []).slice().reverse();
        return mount('projectPhasesChart', 'projectPhasesChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
            grid: { left: 110, right: 24, top: 16, bottom: 24 },
            xAxis: {
                type: 'value',
                minInterval: 1,
                axisLabel: { fontSize: 11, color: '#7e8896' },
                splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } }
            },
            yAxis: {
                type: 'category',
                data: labs,
                axisLabel: { fontSize: 11, color: '#626d7d' }
            },
            series: [{
                type: 'bar',
                data: vals,
                barMaxWidth: 20,
                itemStyle: { color: CYAN, borderRadius: [0, 6, 6, 0] },
                label: { show: true, position: 'right', fontSize: 11, color: '#626d7d' }
            }]
        }, 300);
    };

    // سازگاری با API قبلی (در صورت استفاده صفحات دیگر)
    window.renderUserRegistrationChart = function (labels, values) {
        mount('userRegistrationChart', 'userChartInstance', {
            textStyle: baseTextStyle(),
            series: [{ type: 'line', data: values || [], smooth: true, itemStyle: { color: PRIMARY } }],
            xAxis: { type: 'category', data: labels || [] },
            yAxis: { type: 'value', minInterval: 1 },
            tooltip: { trigger: 'axis' },
            grid: { left: 40, right: 16, top: 24, bottom: 48 }
        }, 280);
    };
    window.renderStoreRegistrationChart = window.renderUserRegistrationChart;
    window.renderRoleDistributionChart = function (l, v) { window.renderOrderStatusChart(l, v); };
    window.renderStoreStatusChart = function (l, v) { window.renderOrdersDailyChart(l, v); };
    window.renderComparisonChart = function () { };
    window.renderDailyActivityChart = function (l, v) { window.renderOrdersDailyChart(l, v); };
    window.renderCouponDistributionChart = function (l, v) { window.renderTopCouponsChart(l, v); };
    window.renderDailyLoginChart = function (l, v) { window.renderSalesTrendChart(l, v); };

    window.renderPointsDailyChart = function (labels, approved, pending) {
        return mount('pointsDailyChart', 'pointsDailyChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: { trigger: 'axis' },
            legend: { bottom: 0, textStyle: { fontSize: 12 } },
            grid: { left: 40, right: 16, top: 24, bottom: 48 },
            xAxis: {
                type: 'category',
                data: labels || [],
                axisLabel: { rotate: 35, fontSize: 10, color: '#7e8896' }
            },
            yAxis: {
                type: 'value',
                minInterval: 1,
                axisLabel: { fontSize: 11, color: '#7e8896' },
                splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } }
            },
            series: [
                { name: 'تاییدشده', type: 'bar', barMaxWidth: 14, data: approved || [], itemStyle: { color: GREEN, borderRadius: [4, 4, 0, 0] } },
                { name: 'در انتظار', type: 'bar', barMaxWidth: 14, data: pending || [], itemStyle: { color: AMBER, borderRadius: [4, 4, 0, 0] } }
            ]
        }, 280);
    };

    window.renderPointsStatusChart = function (labels, values) {
        return mount('pointsStatusChart', 'pointsStatusChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: { trigger: 'item', formatter: function (p) { return p.name + ': ' + formatNum(p.value); } },
            legend: { bottom: 0, textStyle: { fontSize: 12 } },
            series: [{
                type: 'pie',
                radius: ['45%', '70%'],
                center: ['50%', '45%'],
                label: { show: false },
                data: (labels || []).map(function (l, i) {
                    return { name: l, value: (values && values[i] != null) ? values[i] : 0, itemStyle: { color: [GREEN, AMBER, RED][i] || PRIMARY } };
                })
            }]
        }, 280);
    };

    window.renderPointsTopMissionsChart = function (labels, values) {
        var labs = (labels || []).slice().reverse();
        var vals = (values || []).slice().reverse();
        return mount('pointsTopMissionsChart', 'pointsTopMissionsChartInstance', {
            textStyle: baseTextStyle(),
            tooltip: { trigger: 'axis' },
            grid: { left: 110, right: 28, top: 16, bottom: 16 },
            xAxis: { type: 'value', minInterval: 1, axisLabel: { fontSize: 11, color: '#7e8896' }, splitLine: { lineStyle: { color: 'rgba(0,0,0,0.06)' } } },
            yAxis: { type: 'category', data: labs, axisLabel: { fontSize: 11, color: '#626d7d' } },
            series: [{
                type: 'bar',
                data: vals,
                barMaxWidth: 20,
                itemStyle: { color: PRIMARY, borderRadius: [0, 6, 6, 0] },
                label: { show: true, position: 'right', fontSize: 11, color: '#626d7d' }
            }]
        }, 280);
    };

    var ALL_KEYS = [
        'salesTrendChartInstance', 'salesMixChartInstance',
        'orderStatusChartInstance', 'ordersDailyChartInstance',
        'topCouponsChartInstance', 'chanceBoxChartInstance',
        'userRolesChartInstance', 'userRegTrendChartInstance',
        'ticketStatusChartInstance', 'loginTrendChartInstance',
        'projectStatusChartInstance', 'projectTasksChartInstance', 'projectPhasesChartInstance',
        'pointsDailyChartInstance', 'pointsStatusChartInstance', 'pointsTopMissionsChartInstance',
        'userChartInstance'
    ];

    window.addEventListener('resize', function () {
        ALL_KEYS.forEach(function (k) {
            if (window[k] && window[k].resize) window[k].resize();
        });
    });
})();
