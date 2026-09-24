---
title: "Hipcall API anahtarı nasıl alınır ve ilk istek nasıl gönderilir?"
description: "Hipcall panelinden API anahtarı oluşturun, ilk isteğinizi gönderin ve dönen yanıtı okuyun."
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

## Bu rehberde ne var?

Hipcall API, bulut santral sisteminizi kendi yazılımınıza bağlamanızı sağlar. API üzerinden çağrı başlatabilir, çağrı kayıtlarını çekebilir, arayan bilgisini temsilcinin ekranına iletebilir ya da çağrı yönlendirmelerini değiştirebilirsiniz.

Bu sayfada bir API anahtarı oluşturmayı, `/profile` endpoint'i ile anahtarı test etmeyi ve sık karşılaşılan hataları çözmeyi anlatıyoruz.

## Başlamadan önce

API anahtarı oluşturabilmek için hesabınızın Yönetici veya Kurucu rolüne sahip olması gerekir. Diğer roller geliştirici ayarları sayfasına erişemez.

1. `https://use.hipcall.com.tr/` adresinden panele giriş yapın.
2. Sol menüden Ayarlar > Geliştirici sayfasına gidin.
3. API sekmesini açın.
4. Yeni butonuna tıklayın.
5. Anahtara ne için kullandığınızı anlatan bir ad verin (örneğin `CRM Entegrasyonu`).
6. Son geçerlilik tarihi seçin. Aralık 1 gün ile 3 yıl arasındadır, varsayılan 1 yıldır.
7. Oluştur butonuna tıklayın.

Hipcall, anahtarı oluşturduktan hemen sonra tam halini ekranda bir kez gösterir. Sayfadan ayrılmadan önce anahtarı bir şifre yöneticisine kopyalayın. Sonrasında panelde yalnızca maskelenmiş hali görünür.

## İlk isteğinizi gönderin

Anahtarı kod içine yazmak yerine bir çevre değişkeninde saklayın:

```bash
export HIPCALL_API_TOKEN="SFMyNTY.g2gDbQAAAC..."
```

API, standart `Authorization: Bearer` başlığını kullanır. Her şeyin çalıştığını görmek için `/profile` endpoint'ine GET isteği gönderin:

```bash
curl -sS -i \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  https://use.hipcall.com.tr/api/v3/profile
```

## Yanıtı okuma

Geçerli bir anahtar `200 OK` döner. JSON gövdesinde anahtarın bağlı olduğu kullanıcı, atanmış telefon numaraları, varsayılan Caller ID ve hesap limitleri yer alır:

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

Yanıt başlıklarında hız sınırı sayaçları da gelir. Varsayılan limit dakikada 60 istektir.

## Bir şeyler ters giderse

API kimliğinizi doğrulayamazsa `401 Unauthorized` döner. Hata mesajı nedenini söyler.

### Başlık eksik

`Authorization` başlığını eklemeyi unutursanız şu yanıtı alırsınız:

```json
{
  "errors": {
    "detail": "Authentication required. Provide either OAuth Bearer token or API token."
  }
}
```

İsteğinizde `Authorization: Bearer <token>` başlığının olduğundan ve çevre değişkeninin tanımlı olduğundan emin olun.

### Anahtar geçersiz veya süresi dolmuş

Anahtar silinmiş ya da süresi geçmişse:

```json
{
  "errors": {
    "detail": "Not authorized. Check your API token is valid and not expired."
  }
}
```

Panelde Ayarlar > Geliştirici > API sayfasından anahtarın durumunu kontrol edin. Süresi dolmuşsa veya güvenliğinden şüphe ediyorsanız eski anahtarı silip yenisini oluşturun.

## Anahtarınızı güvende tutun

- API anahtarlarını Git reposuna eklemeyin, frontend koduna yazmayın.
- Üretim ortamında anahtarları çevre değişkenleri, Docker Secrets veya bir bulut anahtar kasası ile iletin.
- Bir anahtarın sızdığını düşünüyorsanız panelden silin. Silinen anahtar anında geçersiz olur.

## Parametre listesi

Profil endpoint'i sorgu parametresi veya istek gövdesi almaz:

| Parametre | Konum | Tip | Zorunlu | Açıklama |
|---|---|---|---|---|
| `Authorization` | Header | string | Evet | `Bearer <API_TOKEN>` biçiminde kimlik doğrulama anahtarı. |

## Sonraki adımlar

- Diğer endpoint'leri denemek için [Hipcall API Referansı](https://use.hipcall.com.tr/api-docs/) sayfasına bakın.
- Çağrı kayıtlarını listeleme ve filtreleme adımlarına geçin.
- Sorularınızı [Hipcall Topluluk](https://community.hipcall.com/) forumunda paylaşın.