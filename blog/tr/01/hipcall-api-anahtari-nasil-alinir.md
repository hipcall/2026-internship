---
title: "Hipcall API anahtarı nasıl alınır ve ilk istek nasıl gönderilir?"
description: "Panelden API anahtarı oluşturun, kimlik doğrulamalı ilk isteğinizi gönderin ve dönen cevabı inceleyin."
slug: hipcall-api-anahtari-nasil-alinir
lang: tr
locales: [en, tr]
pubDate: 2026-09-16
categories: [developers]
intent: informational
translationKey: how-to-get-a-hipcall-api-key
tags: [api, authentication, getting-started]
authors: [hipcall-team]
featured: false
draft: true
task: 01
status: review
---

## Genel bakış

Hipcall API, bulut santral altyapınızı kurum içi yazılımlarınıza, CRM platformunuza veya özel iş akışlarınıza entegre etmenizi sağlar. API üzerinden programatik çağrı başlatabilir, çağrı kayıtlarını dışarı aktarabilir, müşteri verilerini anlık olarak temsilcinin ekranına taşıyabilir ve santral yönlendirmelerini yönetebilirsiniz.

Bu rehberde şu temel adımları uyguluyoruz:
- Yönetim panelinde uygun yetki ve geçerlilik süresiyle bir API anahtarı (Personal Access Token) oluşturma.
- API anahtarını çevre değişkenlerinde güvenli şekilde saklama ve `Authorization: Bearer` başlığıyla ilk isteği gönderme.
- `/profile` endpoint'inden dönen kullanıcı, santral numaraları ve hız sınırı (rate limit) meta verilerini inceleme.
- Kimlik doğrulama hatalarını (401 Unauthorized) ve üretim ortamı güvenlik kurallarını yönetme.

## Başlamadan önce

API anahtarı oluşturabilmek için hesabınızın **Yönetici** veya **Kurucu** rolüne sahip olması gerekir. Standart kullanıcı hesapları güvenlik gereği geliştirici ayarlarına erişemez.

1. `https://use.hipcall.com.tr/` adresinden Hipcall yönetim paneline giriş yapın.
2. Sol menüden **Ayarlar > Geliştirici** sayfasına gidin.
3. **API** sekmesini seçin.
4. **Yeni** butonuna tıklayın.
5. Anahtarınız için açıklayıcı bir **Ad** girin (Örn: `CRM Entegrasyonu`).
6. **Son geçerlilik tarihi** belirleyin. Süre en az 1 gün, en fazla 3 yıl olabilir (varsayılan süre 1 yıldır).
7. **Oluştur** butonuna tıklayın.

Oluşturma işleminin hemen ardından Hipcall gizli anahtarın tamamını ekranda gösterir. Sayfadan ayrılmadan önce bu anahtarı güvenli bir şifre yöneticisine kaydedin. Sayfa kapandıktan sonra anahtarın açık hali bir daha görüntülenemez; panelde güvenlik amacıyla yalnızca maskelenmiş biçimi listelenir.

## İlk isteğinizi gönderin

API anahtarınızı betiklerin içine açık metin olarak gömmek yerine bir çevre değişkeninde saklayın:

```bash
export HIPCALL_API_TOKEN="SFMyNTY.g2gDbQAAAC..."
```

API kimlik doğrulaması `Authorization: Bearer <token>` başlığı üzerinden çalışır. İlk bağlantıyı test etmek için profil endpoint'ine bir `GET` isteği gönderin:

```bash
curl -sS -i \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  https://use.hipcall.com.tr/api/v3/profile
```

## Başarılı cevap

Geçerli bir anahtarla istek gönderildiğinde API `200 OK` durum kodu döner.

Dönen JSON gövdesi, anahtarın ait olduğu kullanıcı profilini, atanmış santral numaralarını, varsayılan Caller ID bilgisini ve hesap kısıtlarını içerir:

```json
{
  "data": {
    "user": {
      "id": 4200,
      "owner": true,
      "suspended": false,
      "state": "available",
      "title": null,
      "email": "ahmet@example.com",
      "locale": "tr_TR",
      "timezone": "Europe/Istanbul",
      "created_at": "2026-07-21T10:55:30Z",
      "full_name": "Ahmet Y.",
      "numbers": [
        {
          "id": 938,
          "name": "Müşteri Hizmetleri",
          "number": "+90850XXXXXXX",
          "country": "TR"
        }
      ],
      "default_number": {
        "id": 938,
        "name": "Müşteri Hizmetleri",
        "number": "+90850XXXXXXX",
        "country": "TR"
      },
      "first_name": "Ahmet",
      "last_name": "Y.",
      "phone_prefix": "TR"
    },
    "account": {
      "locale": "tr_TR",
      "timezone": "Europe/Istanbul",
      "contact_center": {
        "b2b": true
      },
      "limits": {
        "max_channel": 5
      }
    },
    "roles": []
  }
}
```

HTTP yanıt başlıklarında hız sınırı (rate limit) sayaçları da iletilir. Standart ortamlarda kota **dakikada 60 istek** olarak tanımlıdır.

## Hata aldığınızda

Kimlik doğrulama aşamasında bir sorunla karşılaşırsanız API `401 Unauthorized` durum kodu üretir. Dönen hata mesajı sorunun kaynağını açıklar:

### 1. Authorization başlığı eksik olduğunda

İstek gönderirken `Authorization` başlığını eklemezseniz API isteği reddeder:

```json
{
  "errors": {
    "detail": "Authentication required. Provide either OAuth Bearer token or API token."
  }
}
```

**Çözüm:** İstek başlıklarınızda `Authorization: Bearer <token>` parametresinin yer aldığından ve token değerinin boş olmadığından emin olun.

### 2. Anahtar geçersiz veya süresi dolmuş olduğunda

Eski, silinmiş veya süresi bitmiş bir anahtar kullanıldığında dönen yanıt:

```json
{
  "errors": {
    "detail": "Not authorized. Check your API token is valid and not expired."
  }
}
```

**Çözüm:** Panelden anahtarın durumunu ve son geçerlilik tarihini kontrol edin. Gerekiyorsa eski anahtarı silip yeni bir anahtar tanımlayın.

## Güvenlik kuralları

- API anahtarınızı Git repolarına, kamuya açık kod bloklarına veya istemci taraflı (tarayıcı / mobil) kaynak kodlarına gömmeyin.
- Servislerinizi yapılandırırken anahtarları Docker Secrets, Kubernetes Secrets veya ortam değişkenleri (`.env`) aracılığıyla iletin.
- Anahtarın güvenliğinden şüphe duyduğunuz an yönetim panelinden ilgili anahtarı silin; silinen anahtar anında geçersiz hale gelir.

## Parametre listesi

Profil endpoint'i herhangi bir sorgu parametresi (query parameter) veya JSON gövdesi gerektirmez:

| Parametre | Konum | Tip | Zorunlu | Açıklama |
|---|---|---|---|---|
| `Authorization` | Header | string | Evet | `Bearer <API_TOKEN>` biçiminde kimlik doğrulama anahtarı. |

## Sonraki adımlar

- Tüm endpoint ve şemaları interaktif olarak denemek için [Hipcall API Referansı](https://use.hipcall.com.tr/api-docs/) sayfasını ziyaret edin.
- Entegrasyon geliştirmeye devam etmek için çağrı kayıtlarını listeleme ve filtreleme adımlarına geçin.
- Sorularınızı ve deneyimlerinizi paylaşmak için [Hipcall Topluluk](https://community.hipcall.com/) forumuna katılın.