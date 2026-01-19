param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath
)

function Wait-ForReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    for ($i = 0; $i -lt 50; $i++) {
        if (Test-Path -Path $Path) {
            $content = Get-Content -Raw -Path $Path
            if ($content -match '</html>') {
                return $content
            }
        }
        Start-Sleep -Milliseconds 200
    }

    return $null
}

function Inject-FailedStyle {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Html
    )

    $css = '<style type="text/css">/* codex-failed-style */ .codex-failed-title{background:#ff3b3b;color:#000;font-weight:700;} .codex-failed-title a{color:#000;font-weight:700;} .codex-failed-assertion{background:#ff3b3b;color:#000;font-weight:700;} .codex-failed-assertion a{color:#000;font-weight:700;} .panel-group[id^="collapse-failure-"] .panel-heading{background:#ff3b3b !important;color:#000 !important;font-weight:700;} .panel-group[id^="collapse-failure-"] .panel-heading a{color:#000 !important;font-weight:700;}</style>'
    $js = '<script>document.addEventListener("DOMContentLoaded",function(){document.querySelectorAll(".panel").forEach(function(panel){var nodes=panel.querySelectorAll(".panel-body .col-md-4");var failValue=null;nodes.forEach(function(n){if(n.textContent.trim()==="Total failed tests"){var next=n.nextElementSibling; if(next && next.classList.contains("col-md-8")){failValue=next;}}}); if(failValue && parseInt(failValue.textContent.trim(),10)>0){var heading=panel.querySelector(".panel-heading"); if(heading){heading.classList.add("codex-failed-title"); heading.style.background="#ff3b3b"; heading.style.color="#000"; heading.style.fontWeight="700";}}});document.querySelectorAll("[id^=\\"collapse-failure-\\"] .panel-heading").forEach(function(heading){heading.classList.add("codex-failed-assertion"); heading.style.background="#ff3b3b"; heading.style.color="#000"; heading.style.fontWeight="700"; var link=heading.querySelector("a"); if(link){link.style.color="#000"; link.style.fontWeight="700";}});});</script>'

    function Apply-InlineFailureStyle {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Content
        )

        $inlineStyle = 'style="background:#ff3b3b;color:#000;font-weight:700;"'
        $Content = [regex]::Replace(
            $Content,
            '(<div class="panel-group" id="collapse-failure-[^"]+".*?<div class="panel-heading")([^>]*?)>',
            { param($m) "$($m.Groups[1].Value) $inlineStyle$($m.Groups[2].Value)>" },
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )
        $Content = [regex]::Replace(
            $Content,
            '(<div class="panel-group" id="collapse-failure-[^"]+".*?<div class="panel-heading".*?<a)([^>]*?)>',
            { param($m) "$($m.Groups[1].Value) style=`"color:#000;font-weight:700;`"$($m.Groups[2].Value)>" },
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

        return $Content
    }

    $hasCss = $Html -match 'codex-failed-style'
    $hasFailureJs = $Html -match '(?s)<script>.*codex-failed-assertion.*?</script>'

    if ($hasCss) {
        $Html = $Html -replace '(?s)<style type="text/css">/\* codex-failed-style \*/.*?</style>', $css
        $scriptPattern = '(?s)<script>.*?codex-failed-title.*?</script>'
        if ($Html -match $scriptPattern -or (-not $hasFailureJs)) {
            $Html = [regex]::Replace($Html, $scriptPattern, '')
            $Html = $Html -replace '</body>', ($js + '</body>')
        }
        return Apply-InlineFailureStyle -Content $Html
    }

    $Html = $Html -replace '</head>', ($css + '</head>')
    $Html = [regex]::Replace($Html, '(?s)<script>.*?codex-failed-title.*?</script>', '')
    $Html = $Html -replace '</body>', ($js + '</body>')

    return Apply-InlineFailureStyle -Content $Html
}

for ($attempt = 0; $attempt -lt 5; $attempt++) {
    $html = Wait-ForReport -Path $ReportPath
    if ([string]::IsNullOrEmpty($html)) {
        Start-Sleep -Milliseconds 200
        continue
    }

    $updated = Inject-FailedStyle -Html $html
    if ($updated -ne $html) {
        try {
            Set-Content -Path $ReportPath -Value $updated
        } catch {
            Start-Sleep -Milliseconds 200
            continue
        }
    }

    $verify = Get-Content -Raw -Path $ReportPath
    if ($verify -match 'codex-failed-style') {
        break
    }

    Start-Sleep -Milliseconds 200
}
